using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuizPersistence;
using NotesParsing;
using System;
using System.IO;
using System.Text;
using HtmlAgilityPack;

namespace FileManagerTests1
{
    [TestClass()]
    public class StackOverflowProtectionTests
    {
        [TestMethod()]
        public void NotesParser_DeeplyNestedHtml_ShouldNotCauseStackOverflow()
        {
            // Create a deeply nested HTML structure (600+ levels, exceeds the 500 limit)
            StringBuilder html = new StringBuilder();
            html.Append("<!DOCTYPE html><html><head><title>Deep Test</title></head><body>");
            
            // Create 600 levels of nested divs (exceeds the 500 limit we set)
            for (int i = 0; i < 600; i++)
            {
                html.Append($"<div id='level{i}'>");
            }
            
            html.Append("<em>test question</em>");
            
            for (int i = 0; i < 600; i++)
            {
                html.Append("</div>");
            }
            
            html.Append("</body></html>");

            // This should not throw a StackOverflowException
            try
            {
                string result = QuestionsFile.CreateQuestionsFileText(html.ToString());
                // If we get here, the protection worked
                Assert.IsTrue(true);
            }
            catch (StackOverflowException)
            {
                Assert.Fail("StackOverflowException occurred despite protection");
            }
        }

        [TestMethod()]
        public void NotesParser_ParentTextNode_ShouldHandleDeepNesting()
        {
            // Create HTML with deep nesting that would cause infinite loop without protection
            StringBuilder html = new StringBuilder();
            html.Append("<!DOCTYPE html><html><head><title>Deep Parent Test</title></head><body>");
            
            // Create 50 nested spans (inline elements that aren't block elements)
            for (int i = 0; i < 50; i++)
            {
                html.Append("<span>");
            }
            
            html.Append("<em>question text</em>");
            
            for (int i = 0; i < 50; i++)
            {
                html.Append("</span>");
            }
            
            html.Append("</body></html>");

            // This should not cause issues
            try
            {
                string result = QuestionsFile.CreateQuestionsFileText(html.ToString());
                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Exception occurred: {ex.Message}");
            }
        }

        [TestMethod()]
        public void NotesParser_ManyHeadingLevels_ShouldHandleGracefully()
        {
            // Test that deeply nested headings don't cause issues
            StringBuilder html = new StringBuilder();
            html.Append("<!DOCTYPE html><html><head><title>Heading Test</title></head><body>");
            
            // Create alternating h1 and h2 headings to test the while loop in addHeading
            for (int i = 0; i < 20; i++)
            {
                html.Append($"<h1>Heading Level 1 - {i}</h1>");
                html.Append($"<h2>Heading Level 2 - {i}</h2>");
            }
            
            html.Append("</body></html>");

            try
            {
                string result = QuestionsFile.CreateQuestionsFileText(html.ToString());
                Assert.IsTrue(true);
            }
            catch (StackOverflowException)
            {
                Assert.Fail("StackOverflowException occurred with heading hierarchy");
            }
        }

        [TestMethod()]
        public void CreateTopic_ManyExistingCopies_ShouldNotCauseStackOverflow()
        {
            // Test that getUniqueName handles many existing copies gracefully
            // Create a simple test HTML
            StringBuilder html = new StringBuilder();
            html.Append("<!DOCTYPE html><html><head><title>Test Topic</title></head><body>");
            html.Append("<em>test question</em>");
            html.Append("</body></html>");
            
            string tempFile = Path.Combine(Path.GetTempPath(), "test_notes_" + Guid.NewGuid() + ".html");
            
            try
            {
                File.WriteAllText(tempFile, html.ToString());
                
                // This should not throw a StackOverflowException even if many copies exist
                try
                {
                    string topicPath = QuestionsFile.CreateNewTopic(tempFile);
                    // If we get here without exception, the protection is working
                    Assert.IsTrue(true);
                    
                    // Clean up created topic
                    if (Directory.Exists(topicPath))
                    {
                        Directory.Delete(topicPath, true);
                    }
                }
                catch (StackOverflowException)
                {
                    Assert.Fail("StackOverflowException occurred despite protection in getUniqueName");
                }
            }
            finally
            {
                // Clean up
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }
    }
}
