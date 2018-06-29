using System;
using System.IO;
using System.Data.Entity;
using System.Collections.Generic;
using System.Linq;
using Musegician.Database;
using IPrivateTagCleanupRequestHandler = Musegician.PrivateTagCleanup.IPrivateTagCleanupRequestHandler;

using LoadingUpdater = Musegician.LoadingDialog.LoadingDialog.LoadingUpdater;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Musegician
{
    public partial class FileManager : IPrivateTagCleanupRequestHandler
    {
        IEnumerable<string> IPrivateTagCleanupRequestHandler.GetAllPrivateTagOwners(LoadingUpdater updater)
        {
            updater.SetTitle("Searching All Files For Private Tag Owners");
            int count = db.Recordings.Count();
            updater.SetSubtitle($"Scanning Recording Tags...  (0/{count})");

            updater.SetLimit(count);

            HashSet<string> owners = new HashSet<string>();

            int i = 0;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (Recording recording in db.Recordings)
            {
                i++;
                if (stopwatch.ElapsedMilliseconds > Settings.BarUpdatePeriod)
                {
                    updater.SetProgress(i);

                    updater.SetSubtitle($"Scanning Recording Tags...  ({i}/{count})");
                    stopwatch.Restart();
                }
                
                try
                {
                    using (TagLib.File file = TagLib.File.Create(recording.Filename))
                    {
                        if (file.GetTag(TagLib.TagTypes.Id3v2, true) is TagLib.Id3v2.Tag id3Tag)
                        {
                            foreach (var privFrame in id3Tag.GetFrames().OfType<TagLib.Id3v2.PrivateFrame>().ToList())
                            {
                                if (!string.IsNullOrEmpty(privFrame.Owner))
                                {
                                    owners.Add(privFrame.Owner);
                                }
                            }
                        }
                    }
                }
                catch (TagLib.UnsupportedFormatException)
                {
                    Console.WriteLine("Skipping UNSUPPORTED FILE: " + recording.Filename);
                    Console.WriteLine(String.Empty);
                    Console.WriteLine("---------------------------------------");
                    Console.WriteLine(String.Empty);
                    continue;
                }
                catch (TagLib.CorruptFileException)
                {
                    Console.WriteLine("Skipping CORRUPT FILE: " + recording.Filename);
                    Console.WriteLine(String.Empty);
                    Console.WriteLine("---------------------------------------");
                    Console.WriteLine(String.Empty);
                    continue;
                }
                catch (IOException)
                {
                    Console.WriteLine("Skipping FILE IN USE: " + recording.Filename);
                    Console.WriteLine(String.Empty);
                    Console.WriteLine("---------------------------------------");
                    Console.WriteLine(String.Empty);
                    continue;
                }
            }

            return owners;
        }

        void IPrivateTagCleanupRequestHandler.CullPrivateTagsByOwner(
            LoadingUpdater updater,
            IEnumerable<string> tagOwners)
        {
            updater.SetTitle("Deleting Selected Private Tags");
            int count = db.Recordings.Count();
            updater.SetSubtitle($"Scanning and Updating Recordings...  (0/{count})\n");
            updater.SetLimit(count);

            HashSet<string> owners = new HashSet<string>(tagOwners);

            int i = 0;
            int modifiedCount = 0;
            string modifiedString = "";
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (Recording recording in db.Recordings)
            {
                i++;
                if (stopwatch.ElapsedMilliseconds > Settings.BarUpdatePeriod)
                {
                    updater.SetProgress(i);
                    if (modifiedCount > 0)
                    {
                        modifiedString = $"Modified {modifiedCount} Files.";
                    }

                    updater.SetSubtitle($"Scanning and Updating Recordings...  ({i}/{count})\n{modifiedString}");
                    stopwatch.Restart();
                }

                try
                {
                    using (TagLib.File file = TagLib.File.Create(recording.Filename))
                    {
                        if (file.GetTag(TagLib.TagTypes.Id3v2, true) is TagLib.Id3v2.Tag id3Tag)
                        {
                            bool write = false;

                            foreach (var privFrame in id3Tag.GetFrames().OfType<TagLib.Id3v2.PrivateFrame>().ToList())
                            {
                                if (string.IsNullOrEmpty(privFrame.Owner) ||
                                    privFrame.PrivateData == null ||
                                    privFrame.PrivateData.Data.Length == 0 ||
                                    owners.Contains(privFrame.Owner))
                                {
                                    write = true;
                                    id3Tag.RemoveFrame(privFrame);
                                }
                            }

                            if (write)
                            {
                                file.Mode = TagLib.File.AccessMode.Write;
                                file.Save();

                                modifiedCount++;
                            }
                        }
                    }
                }
                catch (TagLib.UnsupportedFormatException)
                {
                    Console.WriteLine("Skipping UNSUPPORTED FILE: " + recording.Filename);
                    Console.WriteLine(String.Empty);
                    Console.WriteLine("---------------------------------------");
                    Console.WriteLine(String.Empty);
                    continue;
                }
                catch (TagLib.CorruptFileException)
                {
                    Console.WriteLine("Skipping CORRUPT FILE: " + recording.Filename);
                    Console.WriteLine(String.Empty);
                    Console.WriteLine("---------------------------------------");
                    Console.WriteLine(String.Empty);
                    continue;
                }
                catch (IOException)
                {
                    Console.WriteLine("Skipping FILE IN USE: " + recording.Filename);
                    Console.WriteLine(String.Empty);
                    Console.WriteLine("---------------------------------------");
                    Console.WriteLine(String.Empty);
                    continue;
                }
            }
        }
    }
}
