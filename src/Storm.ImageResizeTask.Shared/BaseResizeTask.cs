using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Storm.ImageResizeTask
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

				List<ITaskItem> outputItems = new List<ITaskItem>();
			    foreach (List<TImageInfo> images in groupedItems.Select(x => x.Value))
			    {
				    GenerateImages(images);

					outputItems.AddRange(images.Select(x => x.Item ?? new TaskItem(x.Output)));
			    }
				
			    OutputImages = outputItems.ToArray();
		    }
		    catch (Exception ex)
		    {
			    Log.LogError(ex.ToString());
		    }

		    return !Log.HasLoggedErrors;
	    }

	    private void GenerateImages(List<TImageInfo> images)
	    {
		    images = images.OrderByDescending(x => x.SizeFactor).ToList();

		    TImageInfo refImage = images.FirstOrDefault(x => x.Item != null);

		    if (refImage == null)
		    {
			    throw new InvalidOperationException("No reference image to resize");
		    }

		    Image sourceImage = Image.FromFile(refImage.Item.ItemSpec);
		    foreach (TImageInfo image in images.Where(x => x.Item == null))
		    {
			    double ratio = image.SizeFactor/refImage.SizeFactor;

			    Bitmap result = ResizeImage(sourceImage, (int) Math.Round(sourceImage.Width*ratio), (int) Math.Round(sourceImage.Height*ratio));
				Log.LogMessage(MessageImportance.High, $"Try to create image {image.Output}");

				//check directory exists
			    string directory = Path.GetDirectoryName(image.Output);
			    if (!string.IsNullOrEmpty(directory))
			    {
				    if (!Directory.Exists(directory))
				    {
					    Directory.CreateDirectory(directory);
				    }
			    }

			    result.Save(image.Output, ImageFormat.Png);
		    }
	    }

		/// <summary>
		/// Resize the image to the specified width and height.
		/// </summary>
		/// <param name="image">The image to resize.</param>
		/// <param name="width">The width to resize to.</param>
		/// <param name="height">The height to resize to.</param>
		/// <returns>The resized image.</returns>
		public static Bitmap ResizeImage(Image image, int width, int height)
		{
			Rectangle destRect = new Rectangle(0, 0, width, height);
			Bitmap destImage = new Bitmap(width, height);

			destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

			using (Graphics graphics = Graphics.FromImage(destImage))
			{
				graphics.CompositingMode = CompositingMode.SourceCopy;
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.SmoothingMode = SmoothingMode.HighQuality;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

				using (ImageAttributes wrapMode = new ImageAttributes())
				{
					wrapMode.SetWrapMode(WrapMode.TileFlipXY);
					graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
				}
			}

			return destImage;
		}

		protected abstract string GetIdentifierForImage(string filePath);

	    protected abstract TImageInfo GetImageInformation(ITaskItem image);

		protected abstract void AddMissingImageFormats(string id, List<TImageInfo> images);
    }
}

