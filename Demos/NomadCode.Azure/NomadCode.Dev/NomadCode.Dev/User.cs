using NomadCode.Azure;

namespace NomadCode.Dev
{
	public class User : AzureEntity
	{
		public int Age { get; set; }
		public string Name { get; set; }
	}
}
