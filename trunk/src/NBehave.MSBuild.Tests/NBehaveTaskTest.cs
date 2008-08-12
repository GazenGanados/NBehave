﻿using System;
using System.Diagnostics;
using Microsoft.Win32;
using NBehave.MSBuild;
using NBehave.Narrator.Framework;
using NUnit.Framework;
using System.IO;


namespace NBehave.MSBuild.Tests
{
    [Theme("A theme to use to test the MSBuildTask")]
    public class AThemeClass
    {
        [Story]
        public void AStory()
        {
            var story = new Story("Test NBehave's MSBuild Task");

            story.AsA("NBehave committer").
                IWant("To test the MSBuild task for NBehave").
                SoThat("I know it works");

            story.WithScenario("A scenario that doesnt do anything").
                Given("a given", 0, a => { }).
                When("event occurs", 0, a => { }).
                Then("there's an outcome", 0, a => { });
        }
    }

    [TestFixture]
    public class NBehaveTaskTest
    {
        [Test]
        public void ShouldExecuteTheOneStory()
        {
            var storyAssemblies = new string[] { this.GetType().Assembly.Location };
            var outputPath = Path.Combine(Path.GetDirectoryName(storyAssemblies[0]), "result.xml");

            var task = new NBehaveTask { DryRun = false, FailBuild = true, StoryOutputPath = outputPath, TestAssemblies = storyAssemblies };

            task.Execute();
            Assert.AreEqual(1, task.StoryResults.NumberOfPassingScenarios);
        }

        [Test]
        public void ShouldExecuteStorySuccessfullyViaMsbuildEXE()
        {
            string msbuild = Path.Combine(
                                    Path.Combine((string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\.NETFramework", "InstallRoot", string.Empty),
                                    "v3.5"),
                                "MSBuild.exe");

            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo(msbuild, "nbehaveTaskTestScript.msbuild");
                process.StartInfo.WorkingDirectory = Path.GetDirectoryName(this.GetType().Assembly.Location);
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();
                using (StreamReader sr = process.StandardOutput)
                {
                    process.WaitForExit();

                    string result = sr.ReadToEnd();
                    string[] results = result.Split(new char[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);
                    Assert.Contains("Build succeeded.",results);
                    Assert.Contains("  Scenarios run: 1, Failures: 0, Pending: 0", results);
                }
            }
        }
    }
}

