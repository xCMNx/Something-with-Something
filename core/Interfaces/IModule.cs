using System.ComponentModel;
using System.Threading.Tasks;

namespace core.Interfaces
{
	public interface IModule : INotifyPropertyChanged
	{
		Task<bool> UpdateAsync(ParametersRequest onParametersRequest, ShowTextRequest showTextRequest);
	}

	public interface IModuleConfigurable : INotifyPropertyChanged
	{
		Task<bool> ConfigurateAsync(ParametersRequest onParametersRequest, ShowTextRequest showTextRequest);
	}
}
