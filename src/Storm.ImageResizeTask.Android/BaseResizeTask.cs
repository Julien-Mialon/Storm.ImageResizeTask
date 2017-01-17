using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Storm.ImageResizeTask.Android
{
    public abstract class BaseResizeTask<TImageInfo> : Task where TImageInfo : IImageInfo
    {
		[Required]
		public ITaskItem[] InputImages { get; set; }

		[Output]
		public ITaskItem[] OutputImages { get; set; }

	    public override bool Execute()
	    {
		    if (InputImages == null || InputImages.Length == 0)
		    {
			    return true;
		    }

		    try
		    {
			    Dictionary<string, List<TImageInfo>> groupedItems = new Dictionary<string, List<TImageInfo>>();
			    foreach (ITaskItem inputImage in InputImages)
			    {
				    string id = GetIdentifierForImage(inputImage.ItemSpec);

				    if (!groupedItems.ContainsKey(id))
				    {
					    groupedItems.Add(id, new List<TImageInfo>());
				    }

				    TImageInfo imageInfo = GetImageInformation(inputImage);
				    imageInfo.Item = inputImage;
				    groupedItems[id].Add(imageInfo);
			    }

			    foreach (KeyValuePair<string, List<TImageInfo>> pair in groupedItems)
			    {
				    AddMissingImageFormats(pair.Key, pair.Value);
			    }


		    }
		    catch (Exception ex)
		    {
			    Log.LogError(ex.ToString());
		    }

		    return !Log.HasLoggedErrors;
	    }

	    protected abstract string GetIdentifierForImage(string filePath);

	    protected abstract TImageInfo GetImageInformation(ITaskItem image);

		protected abstract void AddMissingImageFormats(string id, List<TImageInfo> images);
    }

	public class AndroidResizeTask : BaseResizeTask<AndroidImageInfo>
	{
		private static readonly List<Tuple<string, AndroidImageSize>> SuffixForSizes = new List<Tuple<string, AndroidImageSize>>
		{
			Tuple.Create("-ldpi", AndroidImageSize.Ldpi),
			Tuple.Create("-mdpi", AndroidImageSize.Mdpi),
			Tuple.Create("-tvdpi", AndroidImageSize.Tvdpi),
			Tuple.Create("-hdpi", AndroidImageSize.Hdpi),
			Tuple.Create("-xhdpi", AndroidImageSize.Xhdpi),
			Tuple.Create("-xxhdpi", AndroidImageSize.Xxhdpi),
			Tuple.Create("-xxxhdpi", AndroidImageSize.Xxxhdpi)
		};

		/// <summary>
		/// A comma separated list of image formats in the list
		/// ldpi,mdpi,tvdpi,hdpi,xhdpi,xxhdpi,xxxhdpi
		/// </summary>
		public string OutputFormats { get; set; }

		protected override string GetIdentifierForImage(string filePath)
		{
			string fileName = Path.GetFileName(filePath);
			string directory = Path.GetFileName(Path.GetDirectoryName(filePath));

			if (directory == null)
			{
				Log.LogError($"Invalid directory for item {filePath}");
				throw new InvalidOperationException();
			}

			if (directory.StartsWith("drawable", StringComparison.InvariantCulture))
			{
				return $"drawable/{fileName}";
			}
			if (directory.StartsWith("mipmap", StringComparison.InvariantCulture))
			{
				return $"mipmap/{fileName}";
			}

			Log.LogError($"Invalid directory for item {filePath}, got {directory}, expected drawable or mipmap");
			throw new InvalidOperationException();
		}
		
		protected override void AddMissingImageFormats(string id, List<AndroidImageInfo> images)
		{
			string folder = Path.GetDirectoryName(id);
			string file = Path.GetFileName(id);

			string root = Path.GetDirectoryName(Path.GetDirectoryName(images.First().Item.ItemSpec));

			if (root == null)
			{
				throw new InvalidOperationException("Cannot determine root folder");
			}

			if (file == null)
			{
				throw new InvalidOperationException($"Invalid file in id {id}");
			}

			foreach (Tuple<string, AndroidImageSize> suffix in SuffixForOutputFormats())
			{
				if (images.All(x => x.Size != suffix.Item2))
				{
					images.Add(new AndroidImageInfo
					{
						Size = suffix.Item2,
						Output = Path.Combine(root, $"{folder}{suffix.Item1}", file)
					});
				}
			}
		}

		protected override AndroidImageInfo GetImageInformation(ITaskItem image)
		{
			string directory = Path.GetFileName(Path.GetDirectoryName(image.ItemSpec));

			if (directory == null)
			{
				throw new InvalidOperationException($"Invalid directory for item {image.ItemSpec}");
			}

			return new AndroidImageInfo
			{
				Size = GetSizeFromDirectory(directory)
			};
		}

		private IEnumerable<Tuple<string, AndroidImageSize>> SuffixForOutputFormats()
		{
			if (string.IsNullOrEmpty(OutputFormats))
			{
				return SuffixForSizes;
			}
			string[] formats = OutputFormats.Split(',');
			return SuffixForSizes.Where(item => formats.Contains(item.Item1.TrimStart('-')));
		}

		private AndroidImageSize GetSizeFromDirectory(string directory)
		{
			foreach (Tuple<string, AndroidImageSize> suffix in SuffixForSizes)
			{
				if (directory.EndsWith(suffix.Item1, StringComparison.InvariantCultureIgnoreCase))
				{
					return suffix.Item2;
				}
			}

			throw new InvalidOperationException($"No size available for directory {directory}");
		}
	}

	public interface IImageInfo
	{
		double SizeFactor { get; }
		
		ITaskItem Item { get; set; }

		string Output { get; }
	}

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

	public enum AndroidImageSize
	{
		None,
		Ldpi, //0.75 x mdpi
		Mdpi, // 1
		Tvdpi, // 1.33 x mdpi
		Hdpi, //  1.5 x mdpi
		Xhdpi, // 2 x mdpi
		Xxhdpi, // 3 x mdpi
		Xxxhdpi // 4 x mdpi
	}
}

