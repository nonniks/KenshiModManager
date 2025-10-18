using System.ComponentModel;
using System.Runtime.CompilerServices;

#nullable enable
namespace KenshiModManager.ViewModels;

public class ViewModelBase : INotifyPropertyChanged
{
  public event PropertyChangedEventHandler? PropertyChanged;

  protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
  {
    PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
    if (propertyChanged == null)
      return;
    propertyChanged((object) this, new PropertyChangedEventArgs(propertyName));
  }

  protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
  {
    if (object.Equals((object) field, (object) value))
      return false;
    field = value;
    this.OnPropertyChanged(propertyName);
    return true;
  }
}
