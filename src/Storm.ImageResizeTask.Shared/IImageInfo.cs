using Microsoft.Build.Framework;

namespace Storm.ImageResizeTask
{
	public interface IImageInfo
	{
		double SizeFactor { get; }
		
		ITaskItem Item { get; set; }

		string Output { get; }
	}
}