using System;
using System.IO;
using System.Data.Entity;
using System.Collections.Generic;
using System.Linq;
using Musegician.Database;
using IPrivateTagCleanupRequestHandler = Musegician.PrivateTagCleanup.IPrivateTagCleanupRequestHandler;

namespace Musegician
{
    public partial class FileManager : IPrivateTagCleanupRequestHandler
    {
        IEnumerable<string> IPrivateTagCleanupRequestHandler.GetAllPrivateTagOwners(
            IProgress<string> textSetter,
            IProgress<int> limitSetter,
            IProgress<int> progressSetter)
        {
            textSetter.Report("Searching Tags...");
            limitSetter.Report(db.Recordings.Count());

            HashSet<string> owners = new HashSet<string>();

            int i = 0;
            foreach (Recording recording in db.Recordings)
            {
                progressSetter.Report(i++);

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
            IProgress<string> textSetter,
            IProgress<int> limitSetter,
            IProgress<int> progressSetter,
            IEnumerable<string> tagOwners)
        {
            textSetter.Report("Writing Tags...");
            limitSetter.Report(db.Recordings.Count());

            HashSet<string> owners = new HashSet<string>(tagOwners);

            int i = 0;
            foreach (Recording recording in db.Recordings)
            {
                progressSetter.Report(i++);

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
