using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using HtmlAgilityPack;
using System.Security.Policy;
using System.Collections;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime;
using Microsoft.VisualBasic.FileIO;
using NotesParsing;
using System.Xml.Linq;

namespace QuizPersistence
{
    /// <summary>
    /// Singleton containing methods and constants, to manage the 'quizGenerator' backend. It follows this file structure:
    /// <code>
    /// main_script.exe
    /// .sources\
    ///         |---------- styles.css
    ///         |---------- script.js
    ///         |
    ///         |---------- *general topic folder\
    ///         |                   |---------- notes.html
    ///         |                   |---------- questions.html
    ///         |                   |---------- state.json
    ///         |
    ///         |---------- ... more topic folders\
    /// </code>
    /// </summary>
    public static class QuestionsFile
    {
        public static readonly string utilFolderName = ".sources";
        public static readonly string utilFolderPath = $".\\{utilFolderName}\\";
        public static readonly string questionsFileName = "questions.html";
        public static readonly string notesFileName = "notes.html";
        public static readonly string stylesFileName = "styles.css";
        public static readonly string scriptFileName = "script.js";
        public static readonly string quizState = "state.json";
        public static readonly string questionsScriptName = "questionsScript.js";

        static readonly string templateSourcesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FileManagers", utilFolderName);
        internal static NotesParser parser;
        static QuestionsFile()
        {
            parser = new NotesParser();
        }

        /// <summary>
        /// Used for determining, which html nodes in the input source nodes are the questions 
        /// </summary>
        public static QuestionNodeParams? QuestionNodeAnalyzer { get; private set; }
        public static void SetQuestionNodeAnalyzer(QuestionNodeParams value)
        {
            parser.questionNodeAnalyzer = value;
            QuestionNodeAnalyzer = value;
        }

        /// <summary>
        /// Creates new topic folder with questions file to be later opened as quiz.
        /// </summary>
        /// <param name="notesPath">File path to the source notes</param>
        /// <returns>Path to the Topic</returns>
        public static string CreateNewTopic(string notesPath)
        {
            HtmlDocument htmlFile = new HtmlDocument();
            htmlFile.Load(notesPath);

            string topicTitle = NotesParser.getHtmlTitle(htmlFile);
            string topicName = Path.GetFileNameWithoutExtension(notesPath);

            string name = getUniqueName((topicTitle.Any()) ? topicTitle : topicName);

            string topicFolderPath = Path.Combine(
                utilFolderPath,
                QuestionsFile.toTopicFileName(name)
            );

            if (Directory.Exists(topicFolderPath))
            {
                File.WriteAllText(Path.Combine(topicFolderPath, notesFileName), topicTitle);
            }
            Directory.CreateDirectory(topicFolderPath);

            string notesMarkdown = htmlFile.DocumentNode.OuterHtml;
            File.WriteAllText(
                Path.Combine(topicFolderPath, notesFileName),
                notesMarkdown
            );
            File.WriteAllText(
                Path.Combine(topicFolderPath, questionsFileName),
                CreateQuestionsFileText(notesMarkdown)
            );

            return topicFolderPath;
        }

        /// <summary>
        /// Deletes the whole topic folder
        /// </summary>
        /// <param name="notesPath">Path to the topics folder</param>
        /// <returns>If the folder was deleted</returns>
        public static bool DeleteTopic(string notesPath)
        {
            if (isTopicFolder(notesPath))
            {
                Directory.Delete(notesPath, true);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Renames the Topic in the Topics fiolder
        /// </summary>
        /// <param name="currentTopicDirectoryPath">Path to the topics directory</param>
        /// <param name="newName">The new Topic name</param>
        /// <returns>New path to the Topics folder with new name, if the <see cref="currentTopicDirectoryPath"/> is valid</returns>
        public static string? RenameTopic(string currentTopicDirectoryPath, string newName)
        {
            return RenameTopic(currentTopicDirectoryPath, newName, 0);
        }

        private static string? RenameTopic(string currentTopicDirectoryPath, string newName, int depth)
        {
            // Prevent stack overflow from too many recursive calls
            if (depth > 1000)
            {
                // Add timestamp to ensure uniqueness and stop recursion
                newName = newName + "_" + DateTime.Now.Ticks;
            }

            var currentTopicName = Path.GetDirectoryName(currentTopicDirectoryPath);
            if (currentTopicName == null) return null;

            string newPath = Path.Combine(currentTopicName, QuestionsFile.toTopicFileName(newName));
            
            // Only recurse if depth limit not exceeded
            if (Directory.Exists(newPath) && depth <= 1000)
            {
                string uniqueName = addCopyToName(newName);
                return RenameTopic(currentTopicDirectoryPath, uniqueName, depth + 1);
            }
            
            if (isTopicFolder(currentTopicDirectoryPath))
            {
                Directory.Move(
                    currentTopicDirectoryPath,
                    newPath
                );

                return newPath;
            }

            return null;
        }

        private static string addCopyToName(string name)
        {
            string[] nameSplit = name.Split('.');
            nameSplit[0] = nameSplit[0] + "_copy";
            return string.Join("", nameSplit);
        }

        private static string getUniqueName(string name)
        {
            return getUniqueName(name, 0);
        }

        private static string getUniqueName(string name, int depth)
        {
            // Prevent stack overflow from too many recursive calls
            if (depth > 1000)
            {
                // Add timestamp to ensure uniqueness if too many copies exist
                return name + "_" + DateTime.Now.Ticks;
            }

            if (!isTopicFolder(Path.Combine(utilFolderPath, toTopicFileName(name)))) return name;
            return getUniqueName(addCopyToName(name), depth + 1);
        }

        /// <summary>
        /// Determines, if the given path leads to valid topics folder
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static bool isTopicFolder(string path)
        {
            string fullPath = Path.GetFullPath(path);
            string fullSourcesPath = Path.GetFullPath(QuestionsFile.utilFolderPath);
            string? name = Path.GetDirectoryName(fullPath);
            if (name == null) return false;
            string fullParentPath = Path.GetFullPath(name);
            return Directory.Exists(path) &&
                    fullPath.Contains(fullSourcesPath) &&
                    fullSourcesPath.Contains(fullParentPath);
        }

        /// <summary>
        /// Initializes the sources folder for the Topics (Quizes data).
        /// </summary>
        /// <returns>Path to the created folder</returns>
        private static string createSourcesFolder()
        {
            var currentDir = Directory.GetCurrentDirectory();
            // Directory.CreateDirectory(utilFolderPath);
            var sourcesCopyer = new RecursiveFolderCopy(templateSourcesPath);
            sourcesCopyer.CopyTo(Directory.GetCurrentDirectory(), recursive: true);

            return Path.Combine(currentDir, Path.GetFileName(templateSourcesPath));
        }

        private static string GetDataFilePath()
        {
            if (Directory.Exists(utilFolderPath)) return utilFolderPath;

            return createSourcesFolder();
        }

        /// <summary>
        /// Gives a collection of Topics in History.
        /// </summary>
        /// <returns>an array of names</returns>
        public static string[] GetTopics()
        {
            return Directory.GetDirectories(GetDataFilePath());
        }

        public static string toTopicFileName(string title)
        {
            return title.Replace(" ", "_");
        }

        public static string GetMarkDown(string markdownPath)
        {
            if (!File.Exists(markdownPath)) throw new Exception("Markdow questions not found!");
            return File.ReadAllText(markdownPath);
        }

        /// <summary>
        /// Stores Quiz State data of a given Quiz.
        /// </summary>
        /// <param name="quizName">Quiz name</param>
        /// <param name="jsonStateData">JSON state data to store</param>
        /// <returns>success status</returns>
        public static bool SaveQuizState(string quizName, string jsonStateData)
        {
            try
            {
                string stateFilePath = Path.Combine(utilFolderPath, quizName, quizState);
                File.WriteAllText(stateFilePath, jsonStateData);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Load Quiz State data of a given Quiz.
        /// </summary>
        /// <param name="quizName">Quiz name</param>
        /// <returns>JSON formatted State data</returns>
        public static LoadedStateToken LoadQuizStateData(string quizName)
        {
            string stateFilePath = Path.Combine(utilFolderPath, quizName, quizState);
            if (!File.Exists(stateFilePath)) 
                return new LoadedStateToken(LoadedState.NotFound, "");
            return new LoadedStateToken(LoadedState.Valid, File.ReadAllText(stateFilePath));
        }

        /// <summary>
        /// State of the loaded state data.
        /// </summary>
        public enum LoadedState { Valid, NotFound };
        /// <summary>
        /// Holds data from loaded state File and a status field.
        /// </summary>
        public record struct LoadedStateToken(LoadedState State, string Data) { }

        /// <summary>
        /// Given a path to quiz return its name
        /// </summary>
        /// <param name="pathToQuiz">Path to the questions file</param>
        /// <returns>Name of the quiz. If null, returns empty string</returns>
        public static string GetQuizName(string? pathToQuiz)
        {
            if (pathToQuiz == null) return "";

            
            DirectoryInfo dirInfo = new DirectoryInfo(pathToQuiz);
            string? dirname;
            if (dirInfo.Parent?.Name == utilFolderName 
                && (string)Path.GetFileName(pathToQuiz) != questionsFileName)
            {
                dirname = dirInfo.Name;
            }
            else
            {
                dirname = dirInfo.Parent?.Name;
            }
            if (dirname == null) throw new Exception("Not a valid Quiz file given!");
            return dirname;
        }

        /// <summary>
        /// Takes a html file with notes, and creates a corresponding html markdown contents of just the quiz parts of the notes in standartised format.
        /// Also uses a config file for the specifics of notes creation.
        /// <example>
        /// <code>
        /// questionsMarkdown = FileManager.QuestionsFile.createQuestionsFile(
        ///         System.IO.File.ReadAllText("..\\FileManagerTests\\src\\basicNotes.html")
        /// );
        /// System.IO.File.WriteAllText(resultsFilePath, questionsMarkdown);
        /// </code>
        /// </example>
        /// </summary>
        public static string CreateQuestionsFileText(string notesMarkdown)
        {
            return parser.CreateQuestionsFileText(notesMarkdown);
        }
    }

    /// <summary>
    /// A class containing data necessary for linking Quizzes.
    /// </summary>
    public class HyperLink
    {
        /// <summary>
        /// The label, of the hyperling, what user sees before clicking
        /// </summary>
        public string LinkLabel;
        /// <summary>
        /// The actual link to the quiz.
        /// </summary>
        public string LinkToQuestions;

        public HyperLink(string label, string link)
        {
            LinkLabel = label;
            LinkToQuestions = link;
        }

        public override string ToString()
        {
            return LinkLabel.Replace("_", " ");
        }
    }
}