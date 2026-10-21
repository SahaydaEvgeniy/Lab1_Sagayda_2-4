using System.Collections.ObjectModel;
using Lab1_Sagayda_2_4.Models;

namespace Lab1_Sagayda_2_4.Services;

public interface IAppStateService
{
    ObservableCollection<Student> Students { get; }
}
