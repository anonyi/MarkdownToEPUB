using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkdownToEPUB
{
    public class EpubGenerator
    {
        /// <summary>
        /// 產生 EPUB 檔案
        /// </summary>
        /// <param name="outputPath">檔案輸出路徑</param>
        /// <param name="title">書名</param>
        /// <param name="author">作者</param>
        /// <param name="chaptersPath">章節路徑</param>
        /// <param name="coverImagePath">書封路徑</param>
        public void CreateEpub(string outputPath, string title, string author, string chaptersPath, string coverImagePath)
        {
            var chapters = ReadChapters(chaptersPath);
            bool hasCover = !string.IsNullOrEmpty(coverImagePath) && File.Exists(coverImagePath);

            using (var epubStream = new FileStream(outputPath, FileMode.Create))
            using (var archive = new ZipArchive(epubStream, ZipArchiveMode.Create))
            {
                // Add mimetype file
                AddTextFileToZip(archive, "mimetype", "application/epub+zip", false);

                // Add container.xml
                AddTextFileToZip(archive, "META-INF/container.xml", CreateContainerXml());

                // Add content.opf
                AddTextFileToZip(archive, "OEBPS/content.opf", CreateContentOpf(title, author, chapters, hasCover));

                // Add toc.ncx
                AddTextFileToZip(archive, "OEBPS/toc.ncx", CreateTocNcx(title, chapters, hasCover));

                // Add cover image
                if (hasCover)
                {
                    var coverEntry = archive.CreateEntry("OEBPS/cover.jpg");
                    using (var coverStream = new FileStream(coverImagePath, FileMode.Open))
                    using (var entryStream = coverEntry.Open())
                    {
                        coverStream.CopyTo(entryStream);
                    }

                    // Add cover.xhtml
                    AddTextFileToZip(archive, "OEBPS/cover.xhtml", CreateCoverHtml(title));
                }

                // Add chapter content
                for (int i = 0; i < chapters.Count; i++)
                {
                    AddTextFileToZip(archive, $"OEBPS/chapter{i + 1}.xhtml", CreateChapterContent(title, chapters[i].Title, chapters[i].Content));
                }

                // Add CSS
                AddTextFileToZip(archive, "OEBPS/styles.css", CreateCss());
            }
        }

        /// <summary>
        /// 讀取章節內容
        /// </summary>
        /// <param name="chaptersPath"></param>
        /// <returns></returns>
        private List<Chapter> ReadChapters(string chaptersPath)
        {
            var chapters = new List<Chapter>();

            // 如果提供的是相對路徑，將其轉換為絕對路徑
            if (!Path.IsPathRooted(chaptersPath))
            {
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string exeDir = Path.GetDirectoryName(exePath);
                chaptersPath = Path.Combine(exeDir, chaptersPath);
            }

            var files = Directory.GetFiles(chaptersPath, "*.md")
                .OrderBy(f =>
                {
                    // 將文件名按照數字排序
                    var fileName = Path.GetFileNameWithoutExtension(f);
                    var match = System.Text.RegularExpressions.Regex.Match(fileName, @"^(\d+)(?:\.(\d+))?");
                    if (match.Success)
                    {
                        int mainNumber = int.Parse(match.Groups[1].Value);
                        int subNumber = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
                        return (mainNumber, subNumber);
                    }
                    return (int.MaxValue, 0); // 對於不符合格式的文件名，將其排在最後
                });

            foreach (var file in files)
            {
                var lines = File.ReadAllLines(file);
                if (lines.Length > 0)
                {
                    // 第一行為標題
                    var title = lines[0].TrimStart('#', ' ', '[').TrimEnd(']');
                    // 其餘為內容
                    var content = string.Join(Environment.NewLine, lines.Skip(1));
                    chapters.Add(new Chapter { Title = title, Content = content });
                }
            }

            return chapters;
        }

        /// <summary>
        /// 將文字檔案加入到 ZIP 檔案中
        /// </summary>
        /// <param name="archive"></param>
        /// <param name="entryName"></param>
        /// <param name="content"></param>
        /// <param name="useUtf8"></param>
        private void AddTextFileToZip(ZipArchive archive, string entryName, string content, bool useUtf8 = true)
        {
            var entry = archive.CreateEntry(entryName);
            using (var writer = new StreamWriter(entry.Open(), useUtf8 ? Encoding.UTF8 : Encoding.ASCII))
            {
                writer.Write(content);
            }
        }

        /// <summary>
        /// 產生 container.xml
        /// </summary>
        /// <returns></returns>
        private string CreateContainerXml()
        {
            return @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <container version=""1.0"" xmlns=""urn:oasis:names:tc:opendocument:xmlns:container"">
              <rootfiles>
                <rootfile full-path=""OEBPS/content.opf"" media-type=""application/oebps-package+xml""/>
              </rootfiles>
            </container>";
        }

        /// <summary>
        /// 產生 EPUB 的內容描述文件
        /// </summary>
        /// <param name="title">書名</param>
        /// <param name="author">作者</param>
        /// <param name="chapters">章節</param>
        /// <param name="hasCover">是否有書封</param>
        /// <returns></returns>
        private string CreateContentOpf(string title, string author, List<Chapter> chapters, bool hasCover)
        {
            var sb = new StringBuilder();
            sb.AppendLine($@"<?xml version=""1.0"" encoding=""UTF-8""?>
            <package xmlns=""http://www.idpf.org/2007/opf"" unique-identifier=""BookID"" version=""2.0"">
              <metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"" xmlns:opf=""http://www.idpf.org/2007/opf"">
                <dc:title>{title}</dc:title>
                <dc:creator opf:role=""aut"">{author}</dc:creator>
                <dc:language>en</dc:language>
                <dc:identifier id=""BookID"" opf:scheme=""UUID"">urn:uuid:{Guid.NewGuid()}</dc:identifier>");

            if (hasCover)
            {
                sb.AppendLine("    <meta name=\"cover\" content=\"cover-image\"/>");
            }

            sb.AppendLine("  </metadata>");
            sb.AppendLine("  <manifest>");
            sb.AppendLine("    <item id=\"ncx\" href=\"toc.ncx\" media-type=\"application/x-dtbncx+xml\"/>");
            sb.AppendLine("    <item id=\"css\" href=\"styles.css\" media-type=\"text/css\"/>");

            if (hasCover)
            {
                sb.AppendLine("    <item id=\"cover-image\" href=\"cover.jpg\" media-type=\"image/jpeg\"/>");
                sb.AppendLine("    <item id=\"cover\" href=\"cover.xhtml\" media-type=\"application/xhtml+xml\"/>");
            }

            for (int i = 0; i < chapters.Count; i++)
            {
                sb.AppendLine($"    <item id=\"chapter{i + 1}\" href=\"chapter{i + 1}.xhtml\" media-type=\"application/xhtml+xml\"/>");
            }

            sb.AppendLine("  </manifest>");
            sb.AppendLine("  <spine toc=\"ncx\">");

            if (hasCover)
            {
                sb.AppendLine("    <itemref idref=\"cover\"/>");
            }

            for (int i = 0; i < chapters.Count; i++)
            {
                sb.AppendLine($"    <itemref idref=\"chapter{i + 1}\"/>");
            }

            sb.AppendLine("  </spine>");
            sb.AppendLine("</package>");

            return sb.ToString();
        }

        /// <summary>
        /// 產生目錄文件
        /// </summary>
        /// <param name="title">章節標題</param>
        /// <param name="chapters">章節</param>
        /// <param name="hasCover">是否有封面</param>
        /// <returns></returns>
        private string CreateTocNcx(string title, List<Chapter> chapters, bool hasCover)
        {
            var sb = new StringBuilder();
            sb.AppendLine($@"<?xml version=""1.0"" encoding=""UTF-8""?>
                <ncx xmlns=""http://www.daisy.org/z3986/2005/ncx/"" version=""2005-1"">
                  <head>
                    <meta name=""dtb:uid"" content=""urn:uuid:{Guid.NewGuid()}""/>
                    <meta name=""dtb:depth"" content=""1""/>
                    <meta name=""dtb:totalPageCount"" content=""0""/>
                    <meta name=""dtb:maxPageNumber"" content=""0""/>
                  </head>
                  <docTitle>
                    <text>{title}</text>
                  </docTitle>
                  <navMap>");

            int playOrder = 1;

            if (hasCover)
            {
                sb.AppendLine($@"    <navPoint id=""navpoint-cover"" playOrder=""{playOrder}"">
                  <navLabel>
                    <text>Cover</text>
                  </navLabel>
                  <content src=""cover.xhtml""/>
                </navPoint>");
                playOrder++;
            }

            for (int i = 0; i < chapters.Count; i++)
            {
                sb.AppendLine($@"    <navPoint id=""navPoint-{i + 1}"" playOrder=""{playOrder}"">
                  <navLabel>
                    <text>{chapters[i].Title}</text>
                  </navLabel>
                  <content src=""chapter{i + 1}.xhtml""/>
                </navPoint>");
                playOrder++;
            }

            sb.AppendLine("  </navMap>");
            sb.AppendLine("</ncx>");

            return sb.ToString();
        }

        /// <summary>
        /// 產生章節內容
        /// </summary>
        /// <param name="bookTitle">書名</param>
        /// <param name="chapterTitle">章節標題</param>
        /// <param name="content">內文</param>
        /// <returns></returns>
        private string CreateChapterContent(string bookTitle, string chapterTitle, string content)
        {
            return $@"<?xml version=""1.0"" encoding=""utf-8""?>
                <!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.1//EN"" ""http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd"">
                <html xmlns=""http://www.w3.org/1999/xhtml"">
                <head>
                  <title>{bookTitle} - {chapterTitle}</title>
                  <link rel=""stylesheet"" type=""text/css"" href=""styles.css"" />
                </head>
                <body>
                  <h1>{chapterTitle}</h1>
                  {ConvertMarkdownToHtml(content)}
                </body>
                </html>";
        }

        /// <summary>
        /// 產生 CSS
        /// </summary>
        /// <returns></returns>
        private string CreateCss()
        {
            return @"body {
                  font-family: Arial, sans-serif;
                  margin: 5%;
                  text-align: justify;
                }

                h1 {
                  color: #1a1a1a;
                  text-align: center;
                }

                p {
                  text-indent: 1em;
                  margin-bottom: 1em;
                }";
        }

        /// <summary>
        /// 簡易的 Markdown 轉換為 HTML
        /// </summary>
        /// <param name="markdown"></param>
        /// <returns></returns>
        private string ConvertMarkdownToHtml(string markdown)
        {                       
            var lines = markdown.Split('\n');
            var sb = new StringBuilder();

            foreach (var line in lines)
            {
                if (line.StartsWith("# "))
                {
                    sb.AppendLine($"<h1>{line.Substring(2)}</h1>");
                }
                else if (line.StartsWith("## "))
                {
                    sb.AppendLine($"<h2>{line.Substring(3)}</h2>");
                }
                else if (line.StartsWith("### "))
                {
                    sb.AppendLine($"<h3>{line.Substring(4)}</h3>");
                }
                else if (string.IsNullOrWhiteSpace(line))
                {
                    sb.AppendLine("<p></p>");
                }
                else
                {
                    sb.AppendLine($"<p>{line}</p>");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 產生封面 HTML
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        private string CreateCoverHtml(string title)
        {
            return $@"<?xml version=""1.0"" encoding=""utf-8""?>
                <!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.1//EN"" ""http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd"">
                <html xmlns=""http://www.w3.org/1999/xhtml"">
                <head>
                    <title>{title}</title>
                    <style type=""text/css"">
                        body {{ margin: 0; padding: 0; text-align: center; }}
                        img {{ max-width: 100%; max-height: 100%; }}
                    </style>
                </head>
                <body>
                    <div>
                        <img src=""cover.jpg"" alt=""Cover"" />
                    </div>
                </body>
                </html>";
        }

    }
}
