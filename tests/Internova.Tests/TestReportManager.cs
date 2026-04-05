using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using System;
using System.IO;

namespace Internova.Tests;

public static class TestReportManager
{
    private static ExtentReports? _extent;
    private static readonly string ReportPath = @"C:\Users\sandi\Internova_Reports\Report.html";

    public static ExtentReports Instance
    {
        get
        {
            if (_extent == null)
            {
                InitializeReport();
            }
            return _extent!;
        }
    }

    private static void InitializeReport()
    {
        var directory = Path.GetDirectoryName(ReportPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var spark = new ExtentSparkReporter(ReportPath);
        _extent = new ExtentReports();
        _extent.AttachReporter(spark);
        
        _extent.AddSystemInfo("Environment", "Local");
        _extent.AddSystemInfo("User", Environment.UserName);
    }

    public static void Flush()
    {
        _extent?.Flush();
    }
}
