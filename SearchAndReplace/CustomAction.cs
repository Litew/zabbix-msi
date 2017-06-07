using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Deployment.WindowsInstaller;

namespace CustomActions
{
    public class CustomActions
    {
        /// <summary>
        /// Gets the search and replace data file location. 
        /// This is stored in the user's temp directory
        /// and used by the installer.
        /// </summary>
        /// <value>The search and replace data file.</value>
        private static string SearchAndReplaceDataFile
        {
            get
            {
                return Environment.GetEnvironmentVariable("TEMP") +
                     Path.DirectorySeparatorChar +
                    "SearchAndReplace.xml";
            }
        }

        /// <summary>
        /// This method should be declared with Execute="immediate" 
        /// and called with Before="InstallFinalize"
        /// Use in conjunction with SearchAndReplaceExec
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns></returns>
        [CustomAction]
        public static ActionResult SearchAndReplaceInit(Session session)
        {
            session.Log("Begin SearchAndReplaceInit");
            File.Delete(SearchAndReplaceDataFile);
            if (session.Database.Tables.Contains("SearchAndReplace"))
            {
                var lstSearchAndReplace = new List<SearchAndReplaceData>();
                using (var propertyView =
                 session.Database.OpenView("SELECT * FROM `SearchAndReplace`"))
                {
                    propertyView.Execute();
                    foreach (var record in propertyView)
                    {
                        var token = new SearchAndReplaceData
                        {
                            File = session.Format(record["File"].ToString()),
                            Search = session.Format(record["Search"].ToString()),
                            Replace = session.Format(record["Replace"].ToString())
                        };
                        lstSearchAndReplace.Add(token);
                    }
                }
                var serializer = new TypedXmlSerializer<List<SearchAndReplaceData>>();
                serializer.Serialize(SearchAndReplaceDataFile, lstSearchAndReplace);
            }
            else
            {
                session.Log("No SearchAndReplace custom table found");
            }
            session.Log("End SearchAndReplaceInit");
            return ActionResult.Success;
        }

        /// <summary>
        /// This method should be decleared with Execute="deferred" 
        /// and called with Before="InstallFinalize"
        /// Use in conjunction with SearchAndReplaceInit
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns></returns>
        [CustomAction]
        public static ActionResult SearchAndReplaceExec(Session session)
        {
            session.Log("Begin SearchAndReplaceExec");
            if (File.Exists(SearchAndReplaceDataFile))
            {
                var serializer = new TypedXmlSerializer<List<SearchAndReplaceData>>();
                var tokens = serializer.Deserialize(SearchAndReplaceDataFile);
                tokens.ForEach(token =>
                {
                    try
                    {
                        string fileContents;

                        var file = new FileInfo(token.File);
                        {
                            if (file.Exists)
                            {
                                using (var reader = new StreamReader(file.OpenRead()))
                                {
                                    fileContents = reader.ReadToEnd();
                                    reader.Close();
                                }
                                fileContents = fileContents.Replace(token.Search, token.Replace);

                                using (var writer = new StreamWriter(file.OpenWrite()))
                                {
                                    writer.Write(fileContents);
                                    writer.Flush();
                                    writer.Close();
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        session.Log("Could not process file " + token.File);
                    }
                });
                File.Delete(SearchAndReplaceDataFile);
            }
            session.Log("End SearchAndReplaceExec");
            return ActionResult.Success;
        }
    }
}