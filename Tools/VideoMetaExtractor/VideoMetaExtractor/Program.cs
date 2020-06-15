using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;

namespace VideoMetaExtractor
{
    class Program
    {
        public class Options
        {
            [Value(0, MetaName = "path", HelpText = "Png Directory File Path", Required = true)]
            public string Path { get; set; }
        }

        public static ConcurrentDictionary<string, byte> FilePathMaskTreeDictionary = new ConcurrentDictionary<string, byte>();

        private static void RunExtract(string pngFilePath)
        {
            var stream = File.OpenRead(pngFilePath);

            var argbBytes = PngToArgb.Convert(stream, out var width, out var height);
            var alpha32 = ExtractAlpha.ArgbToAlpha32(argbBytes);
            var maskTree = MaskTree.Make(alpha32, width, height);


            var rootDirectory = Path.GetDirectoryName(pngFilePath);
            var filename = Path.GetFileNameWithoutExtension(pngFilePath);

            var alphaDirectory = Path.Join(rootDirectory, "alpha");
            Directory.CreateDirectory(alphaDirectory);
            var alphaFilePath = Path.Join(alphaDirectory, $"{filename}_alpha.png");
            var bitmap = new Bitmap(width, height, width * 4,
                PixelFormat.Format32bppArgb, Marshal.UnsafeAddrOfPinnedArrayElement(alpha32, 0));
            bitmap.Save(alphaFilePath, ImageFormat.Png);

            FilePathMaskTreeDictionary[pngFilePath] = maskTree;
        }
        static void Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options =>
                {
                    if (string.IsNullOrEmpty(options.Path))
                    {
                        Console.WriteLine("Need path");
                    }

                    var finishedPngCount = 0;

                    var pngFilePaths = Directory.GetFiles(options.Path)
                        .Where(filePath => Path.GetExtension(filePath) == ".png")
                        .ToList();


                    _ = Task.Run(async () =>
                    {
                        var lastPercent = 0;
                        while (pngFilePaths.Count > finishedPngCount)
                        {
                            var percent = finishedPngCount * 100 / pngFilePaths.Count;

                            if (percent != lastPercent)
                            {
                                Console.WriteLine($"Processing - {percent}%");
                                lastPercent = percent;
                            }
                            await Task.Delay(100);
                        }
                    });

                    var tasks = pngFilePaths.Select((pngFilePath) => Task.Run(() =>
                    {
                        RunExtract(pngFilePath);
                        Interlocked.Increment(ref finishedPngCount);
                    }));

                    Task.WaitAll(tasks.ToArray());

                    var maskTrees = pngFilePaths
                        .Select(pngFilePath => FilePathMaskTreeDictionary[pngFilePath])
                        .ToArray();

                    var rootDirectory = Path.GetDirectoryName(pngFilePaths[0]);
                    var maskDirectory = Path.Join(rootDirectory, "mask");
                    Directory.CreateDirectory(maskDirectory);
                    var maskFilePath = Path.Join(maskDirectory, "masktree.byte");
                    File.WriteAllBytes(maskFilePath, maskTrees);
                });
        }
    }
}
