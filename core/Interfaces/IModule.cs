﻿using System.ComponentModel;
using System.Threading.Tasks;

namespace core.Interfaces
{
	public interface IModule : INotifyPropertyChanged
	{
		string Name { get; }
		string ConfigName { get; set; }
		Task<bool> UpdateAsync(ParametersRequest parametersRequest, ShowText showText);
		Task<bool> UpdateSettingsAsync(ParametersRequest parametersRequest, ShowText showText);
	}

	public interface IModuleConfigurable : INotifyPropertyChanged
	{
		Task<bool> ConfigurateAsync(ParametersRequest parametersRequest, ShowText showText);
	}
}
