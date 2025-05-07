using LoaderCore.Interfaces;
using Markdig;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoaderCore.Utilities
{
    internal static class MdToHtmlConverter
    {
        internal static string Convert(string mdFilePath, string? stylesPath = null)
        {
            if (stylesPath == null)
            { 
                var pathProvider = NcetCore.ServiceProvider.GetRequiredService<IFilePathProvider>();
                stylesPath = pathProvider.GetPath("Styles.css");
            }
            string? markdownText;
            string? styles;
            using (StreamReader reader = new(mdFilePath))
            {
                markdownText = reader.ReadToEnd();
            }
            using (StreamReader stylesReader = new(stylesPath))
            {
                styles = stylesReader.ReadToEnd();
            }
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

            var content = Markdown.ToHtml(markdownText, pipeline);

            string htmlContent = $@"<!DOCTYPE html>
                                    <html lang='en'>
                                    <head>
                                    <meta charset='utf-8'>
                                    <style>
                                    {styles}
                                    </style>
                                    </head>
                                    <body>
                                    {content}
                                    </body>
                                    </html>";

            return htmlContent;
        }
    }
}
