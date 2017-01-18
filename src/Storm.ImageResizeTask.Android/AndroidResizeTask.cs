using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;

namespace Storm.ImageResizeTask.Android
{
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
}