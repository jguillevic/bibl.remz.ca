using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace StoriesExtractor
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			using (HttpClient client = new HttpClient())
			{
				var response = await client.GetAsync(@"https://bibl.remz.ca/");
				response.EnsureSuccessStatusCode();
				var byteArray = await response.Content.ReadAsByteArrayAsync();
				var encoding = GetEncodingFromContent(response);
				var htmlContent = encoding.GetString(byteArray);

				var doc = new HtmlDocument();
				doc.LoadHtml(htmlContent);

				var categoryLinkCollection = new HashSet<string>();
				var categoryLinks = doc.DocumentNode.SelectNodes("//a[contains(@class, '0')]");
				if (categoryLinks != null)
				{
					foreach (var categoryLink in categoryLinks)
					{
						var categoryHref = categoryLink.GetAttributeValue("href", string.Empty);
						if (!string.IsNullOrEmpty(categoryHref) && !categoryLinkCollection.Contains(categoryHref))
						{
							categoryLinkCollection.Add(categoryHref);

							var categoryResponse = await client.GetAsync(categoryHref);
							categoryResponse.EnsureSuccessStatusCode();
							var categoryByteArray = await categoryResponse.Content.ReadAsByteArrayAsync();
							var categoryEncoding = GetEncodingFromContent(categoryResponse);
							var categoryHtmlContent = categoryEncoding.GetString(categoryByteArray);

							var categoryDoc = new HtmlDocument();
							categoryDoc.OptionDefaultStreamEncoding = Encoding.GetEncoding("ISO-8859-1");
							categoryDoc.LoadHtml(categoryHtmlContent);

							var pdfLinks = categoryDoc.DocumentNode.SelectNodes("//a[contains(@href, '.pdf')]");

							if (pdfLinks != null)
							{
								foreach (var pdfLink in pdfLinks)
								{
									var fileBookName = HttpUtility.HtmlDecode(pdfLink.GetAttributeValue("href", string.Empty));
									if (!string.IsNullOrEmpty(fileBookName))
									{
										var pdfHref = categoryHref.Replace("index.htm", string.Empty) + fileBookName;

										try
										{
											var pdfResponse = await client.GetAsync(pdfHref);
											pdfResponse.EnsureSuccessStatusCode();
											byte[] fileBytes = await pdfResponse.Content.ReadAsByteArrayAsync();

											File.WriteAllBytes(Path.Combine("books", fileBookName), fileBytes);
										}
										catch
										{
											Console.WriteLine(pdfHref);
										}
									}
								}
							}
						}
					}
				}
			}
		}

		static Encoding GetEncodingFromContent(HttpResponseMessage response)
		{
			return Encoding.GetEncoding("ISO-8859-1");
		}
	}
}
