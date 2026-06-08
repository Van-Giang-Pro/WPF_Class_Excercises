using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace MVVM_Practice.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
		private string firstName;

		public string FirstName
		{
			get { return firstName; }
			set 
			{ 
				firstName = value;
				OnPropertyChanged(nameof(FirstName));
				OnPropertyChanged(nameof(FullName));
			}
		}

		private string lastName;

		public string LastName
		{
			get { return lastName; }
			set 
			{ 
				lastName = value;
				OnPropertyChanged(nameof(LastName));
				OnPropertyChanged(nameof(FullName));
			}
		}

		private string fullName;

		public string FullName
		{
			get => FirstName + " " + LastName;
		}
	}
}
