using System;
using Microsoft.Build.Framework;

namespace Storm.ImageResizeTask.Android
{
	public class AndroidImageInfo : IImageInfo
	{
		public AndroidImageSize Size { get; set; }
		
		public double SizeFactor
		{
			get
			{
				switch (Size)
				{
					case AndroidImageSize.Ldpi:
						return 0.75;
					case AndroidImageSize.Mdpi:
						return 1;
					case AndroidImageSize.Tvdpi:
						return 4/3.0;
					case AndroidImageSize.Hdpi:
						return 1.5;
					case AndroidImageSize.Xhdpi:
						return 2;
					case AndroidImageSize.Xxhdpi:
						return 3;
					case AndroidImageSize.Xxxhdpi:
						return 4;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		public ITaskItem Item { get; set; }

		public string Output { get; set; }
	}
}