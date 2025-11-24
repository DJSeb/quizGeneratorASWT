using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NotesParsing
{
    /// <summary>
    /// Class used for Parsing the Html notes source and geting the questions filed.
    /// </summary>
    public record class NotesParser
    {
        /// <summary>
        /// Base markdown template of an empty Questions html file.
        /// </summary>
        internal static string baseMarkdown = "<!DOCTYPE html><html lang=\"en\" xmlns=\"http://www.w3.org/1999/xhtml\"><head><meta charset=\"utf-8\" /><title></title><script type=\"text/javascript\" language=\"javascript\" src=\".\\..\\script.js\"></script></head><body></body><link rel=\"stylesheet\" href=\".\\..\\styles.css\" /></html>";
        /// <summary>
        /// Template for empty question html node.
        /// </summary>
        internal static string questionTemplate = "<div class=\"question_box\" id=\"question_0\" style=\"display: none\"><h1 class=\"question_name\"></h1><div class=\"question_answer\" style=\"display: none\"></div></div>";
        /// <summary>
        /// Template for empty heading html node.
        /// </summary>
        internal static string headingTemplate = "<div class=\"heading_sections\"><h1 class=\"section_heading\" style=\"display: none\"></h1><div class=\"heading_section_contents\" style=\"display: none\"></div></div>";

        /// <summary>
        /// State of the parsing of the Html file.
        /// </summary>
        NotesParsingState state = new NotesParsingState();
        /// <summary>
        /// State of the result of the parsing of the Html file.
        /// </summary>
        QuizFileResult resultState;
        /// <summary>
        /// Analyses the html node and determines, if it is a question.
        /// </summary>
        public QuestionNodeParams questionNodeAnalyzer;

        private string[] excludedNotesClasses = ["table_of_contents"];

        public NotesParser()
        {
            resultState = new QuizFileResult(state);
            questionNodeAnalyzer = DefaultAnalyzer; 
        }
        public static QuestionNodeParams DefaultAnalyzer { get => new AndQuestionNodeParams().SetName("em"); }

        /// <summary>
        /// Takes a html file with notes, and creates a corresponding html markdown contents of just the quiz parts of the notes in standartised format.
        /// Also uses a config file for the specifics of notes creation.
        /// </summary>
        public string CreateQuestionsFileText(string notesMarkdown)
        {
            HtmlDocument notes = new HtmlDocument();
            HtmlDocument result = new HtmlDocument();
            notes.LoadHtml(notesMarkdown);
            notes = preprocessNotes(notes);
            result.LoadHtml(baseMarkdown);
            resultState.resultCurrentPosition = result.DocumentNode.SelectSingleNode("//body");

            HtmlNode bodyNode = notes.DocumentNode.SelectSingleNode("//body");
            goThrowNodes(bodyNode);

            string resultText = result.DocumentNode.OuterHtml;
            state.Reset();
            resultState.Reset();
            return resultText;
        }
        
        private HtmlDocument preprocessNotes(HtmlDocument notesInput)
        {
            foreach (var excludedNodeClass in excludedNotesClasses) 
            {
                var unwanted = notesInput.DocumentNode.SelectNodes($"//*[contains(concat(' ', normalize-space(@class), ' '), ' {excludedNodeClass.Trim()} ')]");
                if (unwanted == null) continue;
                foreach (var unwantedNode in unwanted)
                {
                    unwantedNode.Remove();
                }
            }

            return notesInput;
        }

        /// <summary>
        /// Recursive function used for iterating for all nested children of a given node.
        /// </summary>
        /// <param name="node">The html node to be searched throw.</param>
        void goThrowNodes(HtmlNode node)
        {
            goThrowNodes(node, 0);
        }

        private void goThrowNodes(HtmlNode node, int depth)
        {
            // Prevent stack overflow from deeply nested HTML structures
            if (depth > 500)
            {
                // Skip processing extremely deep nodes to prevent stack overflow
                return;
            }

            HtmlNodeCollection children = node.ChildNodes;
            bool areInHeading = false;
            HtmlNode? currentHeadingAnswer = null;
            foreach (HtmlNode child in children)
            {
                onNode(child, ref areInHeading, ref currentHeadingAnswer, depth);
            }
        }

        internal virtual void onNode(HtmlNode child, ref bool areInHeading, ref HtmlNode? currentHeadingAnswer)
        {
            onNode(child, ref areInHeading, ref currentHeadingAnswer, 0);
        }

        private void onNode(HtmlNode child, ref bool areInHeading, ref HtmlNode? currentHeadingAnswer, int depth)
        {
            areInHeading = false;
            if (child.NodeType != HtmlNodeType.Element)
            {
                return;
            }

            if (child.Name == "header")
            {
                return;
            }

            // Determine if current node is the looked for question
            bool isQuestion = questionNodeAnalyzer.Analyze(child);

            if (resultState.isHeading(child))
            {
                resultState.addHeading(child);
                areInHeading = true;
                state.noQuestionInHeadingYet = true;
                currentHeadingAnswer = resultState.resultCurrentPosition?.SelectSingleNode("//*[contains(@class, 'heading_section_contents')]");
            }
            else if (isQuestion)
            {
                state.noQuestionInHeadingYet = false;
                resultState.addQuestion(child);
            }

            if (!areInHeading && !isQuestion)
            {
                goThrowNodes(child, depth + 1);
            }

            if (state.currentParentHeadingRank != 0 && state.noQuestionInHeadingYet && !areInHeading && currentHeadingAnswer != null)
            {
                currentHeadingAnswer.AppendChild(child);
            }
        }

        /// <summary>
        /// Gets a title of html page.
        /// </summary>
        /// <param name="htmlFile">html page</param>
        /// <returns>title of html page</returns>
        public static string getHtmlTitle(HtmlDocument htmlFile)
        {
            HtmlNode title = htmlFile.DocumentNode.SelectSingleNode("//title");
            if (title != null)
            {
                return title.InnerText;
            }
            return "";
        }
    }

    /// <summary>
    /// Holds the state of the notes html parsing result. 
    /// </summary>
    internal record class QuizFileResult
    {
        /// <summary>
        /// the resulting questions html contained in a node.
        /// </summary>
        public HtmlNode? resultCurrentPosition;
        /// <summary>
        /// The html parsing state.
        /// </summary>
        private NotesParsingState state;

        public QuizFileResult(NotesParsingState state)
        {
            this.state = state;
            Reset();
        }

        /// <summary>
        /// Resets the state to its initial conditions.
        /// </summary>
        public void Reset()
        {
            resultCurrentPosition = null;
        }

        internal void addQuestion(HtmlNode question)
        {
            if (question == null) { return; }
            HtmlDocument questionBoxTemplate = new HtmlDocument();
            questionBoxTemplate.LoadHtml(NotesParser.questionTemplate);
            addIdCounterToNodeInDoc(questionBoxTemplate);

            HtmlNode latestQuestionBox = questionBoxTemplate
                .DocumentNode
                .SelectSingleNode("//*[contains(@class, 'question_box')]");
            HtmlNode? questionBox = resultCurrentPosition?
                .AppendChild(latestQuestionBox);

            HtmlNode? questionName = questionBox?.SelectSingleNode("//*[contains(@class, 'question_name')]");
            HtmlNode? questionAnswer = questionBox?
                .SelectSingleNode("//*[contains(@class, 'question_answer')]");
            if (questionAnswer != null && questionName != null && questionBox != null)
            {
                questionBox.SelectSingleNode("//*[contains(@class, 'question_name')]").InnerHtml = question.InnerText;
                questionAnswer.AppendChild(ParentTextNode(question));
            }
        }

        internal void addHeading(HtmlNode heading)
        {
            if (heading == null) { return; }
            HtmlDocument headingBoxTemplate = new HtmlDocument();
            headingBoxTemplate.LoadHtml(NotesParser.headingTemplate);
            //addIdCounterToNodeInDoc(headingBoxTemplate);
            int headingRank = getHeadingRank(heading);

            if (headingRank <= state.currentParentHeadingRank)
            {
                HtmlNode? currentParentHeading = resultCurrentPosition?.ParentNode;
                int parentHeadingRank = state.currentParentHeadingRank - 1;
                int maxIterations = 100; // Prevent infinite loop
                int iterations = 0;
                while (parentHeadingRank >= headingRank && currentParentHeading != null)
                {
                    currentParentHeading = currentParentHeading?.ParentNode;
                    parentHeadingRank--;
                    iterations++;
                    if (iterations > maxIterations)
                    {
                        // Prevent infinite loop - break if we've gone too far
                        break;
                    }
                }
                resultCurrentPosition = currentParentHeading;
            }
            HtmlNode? headingBox = resultCurrentPosition?.AppendChild(headingBoxTemplate.DocumentNode.SelectSingleNode("//div"));
            resultCurrentPosition = headingBox?.SelectSingleNode("//*[contains(@class, 'heading_sections')]");
            state.currentParentHeadingRank = headingRank;

            if (headingBox == null) return;
            headingBox.SelectSingleNode("//*[contains(@class, 'section_heading')]").InnerHtml = heading.InnerText;
        }

        void addIdCounterToNodeInDoc(HtmlDocument docForId)
        {
            docForId.DocumentNode
                .SelectSingleNode("//div")
                .SetAttributeValue("id", $"question_{state.idCounter}");
            state.idCounter++;
        }

        private HtmlNode ParentTextNode(HtmlNode keySegment)
        {
            string[] blockElements = { "p", "li", "ul", "ol", "pre", "div", "blockquote", "dl", "figure", "hr", "article", "footer", "header", "main", "section", "dd"  };
            HtmlNode parent = keySegment;
            int maxDepth = 1000; // Prevent infinite loop
            int depth = 0;
            while (!blockElements.Contains(parent.Name) && !isHeading(parent) && parent.ParentNode != null)
            {
                parent = parent.ParentNode;
                depth++;
                if (depth > maxDepth)
                {
                    // If we've gone too deep, return the current node to prevent stack overflow
                    return parent;
                }
            }
            return parent;
        }

        internal bool isHeading(HtmlNode node)
        {
            return getHeadingRank(node) > 0;
        }

        /// <summary>
        /// Takes a HtmlNode and if it is a heading like 'h3', returns it's rank, in our situation '3' or it returns '0', if it isn't a heading node.
        /// </summary>
        private int getHeadingRank(HtmlNode hx)
        {
            int rank = 0;
            if (hx.Name[0] != 'h')
            {
                return 0;
            }

            try
            {
                rank = Convert.ToInt32(hx.Name.Substring(1, hx.Name.Length - 1));
            }
            catch
            {
                return 0;
            }

            return rank;
        }
    }

    /// <summary>
    /// State of the Html parsing.
    /// </summary>
    internal record class NotesParsingState
    {
        public int idCounter;
        public int currentParentHeadingRank;
        public bool noQuestionInHeadingYet;

        /// <summary>
        /// Resets the state to its initial conditions.
        /// </summary>
        public NotesParsingState()
        {
            Reset();
        }

        public void Reset()
        {
            idCounter = 1;
            currentParentHeadingRank = 0;
            noQuestionInHeadingYet = true;
        }
    }
}
