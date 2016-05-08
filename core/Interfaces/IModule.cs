using System.ComponentModel;
using System.Threading.Tasks;

namespace core.Interfaces
{
	public interface IModule : INotifyPropertyChanged
	{
		string Name { get; }
		Task<bool> UpdateAsync(ParametersRequest parametersRequest, ShowText showText);
	}

	public interface IModuleConfigurable : INotifyPropertyChanged
	{
		Task<bool> ConfigurateAsync(ParametersRequest parametersRequest, ShowText showText);
	}
}
