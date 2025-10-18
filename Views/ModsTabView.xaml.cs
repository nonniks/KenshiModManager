using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using KenshiLib.Core;
using KenshiModManager.ViewModels;

namespace KenshiModManager.Views
{
    /// <summary>
    /// Interaction logic for ModsTabView.xaml
    /// Enhanced with professional drag & drop UX (Paradox Launcher-style)
    /// </summary>
    public partial class ModsTabView : UserControl
    {
        // Drag & Drop state
        private ModInfo? _draggedItem;
        private int _draggedIndex = -1;
        private bool _isDragging = false;
        private Point _dragStartPoint;

        // Visual feedback
        private DragAdorner? _dragAdorner;
        private AdornerLayer? _adornerLayer;
        private ScrollViewer? _scrollViewer;

        // Auto-scroll
        private DispatcherTimer? _autoScrollTimer;
        private Point _lastDragPosition;
        private DateTime _scrollStartTime; // Track when continuous scrolling started
        private bool _isScrolling = false; // Track if currently in scroll zone

        // Constants
        private const double DragThreshold = 5.0; // Minimum distance to start drag
        private const double AutoScrollEdgeSize = 40.0; // Scroll zone from edge (40px recommended)
        private const double MinScrollSpeed = 1.0; // Min speed at start
        private const double MaxScrollSpeed = 8.0; // Max speed after full acceleration
        private const double ScrollEasingFactor = 1.5; // Easing curve steepness
        private const double ScrollStartDelay = 0.2; // 200ms delay before scroll starts

        public ModsTabView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Get adorner layer for visual feedback
            _adornerLayer = AdornerLayer.GetAdornerLayer(ModsListBox);

            // Find ScrollViewer in ListBox template (simple search)
            _scrollViewer = FindVisualChild<ScrollViewer>(ModsListBox);

            if (_scrollViewer != null)
            {
                Console.WriteLine($"[ModsTabView] Found ScrollViewer in ListBox template");
            }
            else
            {
                Console.WriteLine("[ModsTabView] WARNING: ScrollViewer not found!");
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            CleanupDragOperation();
        }

        #region LoadOrder TextBox Handlers

        // LoadOrder TextBox validation - only digits
        private void LoadOrderTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Only allow digits
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        // LoadOrder TextBox - reorder mods when user finishes editing
        private void LoadOrderTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is TextBox textBox && textBox.DataContext is ModInfo modInfo && DataContext is ModsTabViewModel viewModel)
                {
                    int newLoadOrder = modInfo.LoadOrder;

                    // Clamp to valid range
                    if (newLoadOrder < 0) newLoadOrder = 0;
                    if (newLoadOrder >= viewModel.ActivePlaysetMods.Count)
                        newLoadOrder = viewModel.ActivePlaysetMods.Count - 1;

                    int currentIndex = viewModel.ActivePlaysetMods.IndexOf(modInfo);

                    if (currentIndex != newLoadOrder && currentIndex >= 0)
                    {
                        // Move mod to new position
                        viewModel.ActivePlaysetMods.Move(currentIndex, newLoadOrder);

                        // Update all LoadOrder values
                        for (int i = 0; i < viewModel.ActivePlaysetMods.Count; i++)
                        {
                            viewModel.ActivePlaysetMods[i].LoadOrder = i;
                        }

                        // Refresh filtered list
                        viewModel.ApplyFilter();

                        // Save changes
                        viewModel.SavePlaysetOrder();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ModsTabView] Error in LoadOrderTextBox_LostFocus: {ex.Message}");
            }
        }

        #endregion

        #region Drag & Drop Handlers

        private void ModsListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // IMPORTANT: Skip drag initialization if clicking on interactive elements
                if (e.OriginalSource is TextBox || e.OriginalSource is CheckBox)
                {
                    Console.WriteLine("[ModsTabView] Click on interactive element - skipping drag initialization");
                    ResetDragState();
                    return;
                }

                if (sender is ListBox listBox && e.OriginalSource != null)
                {
                    var item = FindVisualParent<ListBoxItem>(e.OriginalSource as DependencyObject);
                    if (item != null && item.Content is ModInfo modInfo)
                    {
                        // Store potential drag start
                        _draggedItem = modInfo;
                        _draggedIndex = listBox.Items.IndexOf(modInfo);
                        _dragStartPoint = e.GetPosition(listBox);

                        Console.WriteLine($"[ModsTabView] Prepared drag for: {modInfo.Name} (index {_draggedIndex})");
                    }
                    else
                    {
                        ResetDragState();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ModsTabView] Error in PreviewMouseLeftButtonDown: {ex.Message}");
                ResetDragState();
            }
        }

        private void ModsListBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Cancel any ongoing drag
                CleanupDragOperation();
                ResetDragState();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ModsTabView] Error in PreviewMouseRightButtonDown: {ex.Message}");
            }
        }

        private void ModsListBox_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                // Check if we should start dragging
                if (e.LeftButton == MouseButtonState.Pressed &&
                    _draggedItem != null &&
                    !_isDragging &&
                    sender is ListBox listBox)
                {
                    Point currentPosition = e.GetPosition(listBox);
                    Vector diff = _dragStartPoint - currentPosition;

                    // Check if mouse moved beyond threshold
                    if (Math.Abs(diff.X) > DragThreshold || Math.Abs(diff.Y) > DragThreshold)
                    {
                        InitiateDragOperation(listBox);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ModsTabView] Error in MouseMove: {ex.Message}");
                CleanupDragOperation();
            }
        }

        private void InitiateDragOperation(ListBox listBox)
        {
            if (_isDragging || _draggedItem == null)
                return;

            try
            {
                _isDragging = true;
                Console.WriteLine($"[ModsTabView] Initiating drag for: {_draggedItem.Name}");

                // Create visual feedback adorner
                if (_adornerLayer != null)
                {
                    var draggedElement = GetListBoxItemFromMod(_draggedItem);
                    if (draggedElement != null)
                    {
                        _dragAdorner = new DragAdorner(listBox, draggedElement);
                        _adornerLayer.Add(_dragAdorner);
                    }
                }

                // Start auto-scroll timer
                StartAutoScrollTimer();

                // Perform drag & drop
                var result = DragDrop.DoDragDrop(listBox, _draggedItem, DragDropEffects.Move);

                Console.WriteLine($"[ModsTabView] Drag completed with result: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ModsTabView] Error initiating drag: {ex.Message}");
            }
            finally
            {
                CleanupDragOperation();
            }
        }

        private void ModsListBox_DragOver(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data != null && e.Data.GetDataPresent(typeof(ModInfo)) && sender is ListBox listBox)
                {
                    e.Effects = DragDropEffects.Move;

                    // Update adorner position
                    if (_dragAdorner != null)
                    {
                        Point position = e.GetPosition(listBox);
                        _dragAdorner.Position = new Point(position.X - 20, position.Y - 35);
                    }

                    // Update insertion indicator
                    UpdateInsertionIndicator(e, listBox);

                    // Update last drag position for auto-scroll (relative to ListBox, not ScrollViewer)
                    _lastDragPosition = e.GetPosition(listBox);
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }

                e.Handled = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ModsTabView] Error in DragOver: {ex.Message}");
                e.Effects = DragDropEffects.None;
                e.Handled = true;
            }
        }

        private void ModsListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                // NOTE: Mouse wheel events don't fire during DoDragDrop (WPF limitation)
                // User can scroll normally when not dragging
                if (!_isDragging && _scrollViewer != null)
                {
                    // Allow normal scrolling
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ModsTabView] Error in PreviewMouseWheel: {ex.Message}");
            }
        }

        private void ModsListBox_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data != null &&
                    e.Data.GetDataPresent(typeof(ModInfo)) &&
                    _draggedItem != null &&
                    e.OriginalSource != null &&
                    DataContext is ModsTabViewModel viewModel)
                {
                    var item = FindVisualParent<ListBoxItem>(e.OriginalSource as DependencyObject);

                    if (item != null && item.Content is ModInfo targetItem)
                    {
                        int targetIndex = viewModel.ActivePlaysetMods.IndexOf(targetItem);

                        if (targetIndex >= 0 && targetIndex != _draggedIndex && _draggedIndex >= 0)
                        {
                            Console.WriteLine($"[ModsTabView] Moving mod from {_draggedIndex} to {targetIndex}");

                            // Move item in collection
                            viewModel.ActivePlaysetMods.Move(_draggedIndex, targetIndex);

                            // Update load orders for ALL items
                            for (int i = 0; i < viewModel.ActivePlaysetMods.Count; i++)
                            {
                                viewModel.ActivePlaysetMods[i].LoadOrder = i;
                            }

                            // Refresh filtered list
                            viewModel.ApplyFilter();

                            // Save to playset
                            viewModel.SavePlaysetOrder();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ModsTabView] Error in Drop: {ex.Message}");
            }
            finally
            {
                CleanupDragOperation();
            }
        }

        private void ModsListBox_QueryContinueDrag(object sender, QueryContinueDragEventArgs e)
        {
            try
            {
                // Handle Escape key to cancel drag
                if (e.EscapePressed)
                {
                    e.Action = DragAction.Cancel;
                    Console.WriteLine("[ModsTabView] Drag cancelled by user (Escape)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ModsTabView] Error in QueryContinueDrag: {ex.Message}");
            }
        }

        private void ModsListBox_DragLeave(object sender, DragEventArgs e)
        {
            try
            {
                // Hide insertion indicator when leaving
                if (_dragAdorner != null)
                {
                    _dragAdorner.ShowInsertion = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ModsTabView] Error in DragLeave: {ex.Message}");
            }
        }

        #endregion

        #region Visual Feedback

        private void UpdateInsertionIndicator(DragEventArgs e, ListBox listBox)
        {
            if (_dragAdorner == null)
                return;

            var item = FindVisualParent<ListBoxItem>(e.OriginalSource as DependencyObject);
            if (item != null)
            {
                Point position = e.GetPosition(item);

                // Determine if insertion should be above or below
                bool insertAbove = position.Y < item.ActualHeight / 2;

                Point insertionPoint = insertAbove
                    ? new Point(0, item.TranslatePoint(new Point(0, 0), listBox).Y)
                    : new Point(0, item.TranslatePoint(new Point(0, item.ActualHeight), listBox).Y);

                _dragAdorner.InsertionPosition = insertionPoint;
                _dragAdorner.ShowInsertion = true;
            }
            else
            {
                _dragAdorner.ShowInsertion = false;
            }
        }

        #endregion

        #region Auto-Scroll

        private void StartAutoScrollTimer()
        {
            if (_autoScrollTimer == null)
            {
                _autoScrollTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(30) // Slower interval for smoother scroll
                };
                _autoScrollTimer.Tick += AutoScrollTimer_Tick;
            }

            _autoScrollTimer.Start();
        }

        private void StopAutoScrollTimer()
        {
            _autoScrollTimer?.Stop();
        }

        private void AutoScrollTimer_Tick(object? sender, EventArgs e)
        {
            if (_scrollViewer == null || !_isDragging)
                return;

            try
            {
                // Use cached position from DragOver (relative to ListBox, not ScrollViewer)
                double mouseY = _lastDragPosition.Y;

                // Use ListBox ActualHeight for zone calculations, not ScrollViewer ViewportHeight
                double listBoxHeight = ModsListBox.ActualHeight;
                double extentHeight = _scrollViewer.ExtentHeight;
                double viewportHeight = _scrollViewer.ViewportHeight;

                // Calculate max scroll offset
                double maxOffset = extentHeight - viewportHeight;

                // Check if there's anything to scroll
                if (maxOffset <= 0 || listBoxHeight <= 0)
                    return;

                // Calculate distance from edge and dynamic speed (pressure scrolling)
                double distanceFromEdge = 0;
                bool scrollUp = false;
                bool scrollDown = false;

                if (mouseY >= 0 && mouseY < AutoScrollEdgeSize)
                {
                    // Near top edge - scroll up
                    distanceFromEdge = AutoScrollEdgeSize - mouseY;
                    scrollUp = true;
                }
                else if (mouseY > listBoxHeight - AutoScrollEdgeSize && mouseY <= listBoxHeight)
                {
                    // Near bottom edge - scroll down
                    distanceFromEdge = mouseY - (listBoxHeight - AutoScrollEdgeSize);
                    scrollDown = true;
                }

                // Apply smooth acceleration with easing
                if (scrollUp || scrollDown)
                {
                    // Start timing if entering scroll zone
                    if (!_isScrolling)
                    {
                        _isScrolling = true;
                        _scrollStartTime = DateTime.Now;
                    }

                    // Calculate elapsed time in scroll zone
                    double elapsedSeconds = (DateTime.Now - _scrollStartTime).TotalSeconds;

                    // Apply 200ms delay before starting scroll
                    if (elapsedSeconds < ScrollStartDelay)
                    {
                        return; // Don't scroll yet, still in delay period
                    }

                    // Adjust time for delay
                    double adjustedTime = elapsedSeconds - ScrollStartDelay;

                    // Apply exponential easing: speed = minSpeed + (maxSpeed - minSpeed) * (1 - e^(-k*t))
                    // This creates smooth acceleration that starts slow and gradually speeds up
                    double easedProgress = 1.0 - Math.Exp(-ScrollEasingFactor * adjustedTime);
                    double finalSpeed = MinScrollSpeed + (MaxScrollSpeed - MinScrollSpeed) * easedProgress;

                    Console.WriteLine($"[ModsTabView] AutoScroll: mouseY={mouseY:F1}, elapsed={elapsedSeconds:F2}s, adjusted={adjustedTime:F2}s, eased={easedProgress:F2}, speed={finalSpeed:F2}, up={scrollUp}, down={scrollDown}");

                    if (scrollUp && _scrollViewer.VerticalOffset > 0)
                    {
                        double newOffset = _scrollViewer.VerticalOffset - finalSpeed;
                        _scrollViewer.ScrollToVerticalOffset(Math.Max(0, newOffset));
                    }
                    else if (scrollDown && _scrollViewer.VerticalOffset < maxOffset)
                    {
                        double newOffset = _scrollViewer.VerticalOffset + finalSpeed;
                        _scrollViewer.ScrollToVerticalOffset(Math.Min(maxOffset, newOffset));
                    }
                }
                else
                {
                    // Reset timing when leaving scroll zone
                    _isScrolling = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ModsTabView] Error in AutoScrollTimer_Tick: {ex.Message}");
            }
        }

        #endregion

        #region Cleanup & Utilities

        private void ResetDragState()
        {
            _draggedItem = null;
            _draggedIndex = -1;
            _isDragging = false;
            _dragStartPoint = new Point(0, 0);
            _isScrolling = false; // Reset scroll acceleration timer
        }

        private void CleanupDragOperation()
        {
            try
            {
                // Stop auto-scroll
                StopAutoScrollTimer();

                // Remove adorner
                if (_dragAdorner != null && _adornerLayer != null)
                {
                    _adornerLayer.Remove(_dragAdorner);
                    _dragAdorner = null;
                }

                // Reset state
                ResetDragState();

                Console.WriteLine("[ModsTabView] Drag operation cleaned up");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ModsTabView] Error cleaning up drag operation: {ex.Message}");
            }
        }

        private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            try
            {
                while (child != null)
                {
                    if (child is T parent)
                        return parent;

                    // Use safe call - some elements (like Run) are not Visual
                    try
                    {
                        child = VisualTreeHelper.GetParent(child);
                    }
                    catch
                    {
                        // If GetParent fails (non-Visual element), return null
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ModsTabView] Error in FindVisualParent: {ex.Message}");
            }

            return null;
        }

        private static T? FindVisualChild<T>(DependencyObject? parent) where T : DependencyObject
        {
            if (parent == null)
                return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T result)
                    return result;

                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                    return descendant;
            }

            return null;
        }

        private UIElement? GetListBoxItemFromMod(ModInfo mod)
        {
            if (ModsListBox.ItemContainerGenerator.ContainerFromItem(mod) is ListBoxItem container)
            {
                return container;
            }
            return null;
        }

        #endregion

        #region Other Event Handlers

        // Burger menu button - open context menu programmatically
        private void BurgerMenuButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.ContextMenu != null)
                {
                    button.ContextMenu.PlacementTarget = button;
                    button.ContextMenu.IsOpen = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ModsTabView] Error opening burger menu: {ex.Message}");
            }
        }

        // Toggle switch changed - save playset state
        private void ModToggle_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is CheckBox checkBox &&
                    checkBox.DataContext is ModInfo modInfo &&
                    DataContext is ModsTabViewModel viewModel)
                {
                    Console.WriteLine($"[ModsTabView] Mod '{modInfo.Name}' IsEnabled changed to: {modInfo.IsEnabled}");

                    // Save playset to persist the enabled/disabled state
                    viewModel.SavePlaysetOrder();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ModsTabView] Error in ModToggle_Changed: {ex.Message}");
            }
        }

        #endregion
    }
}
