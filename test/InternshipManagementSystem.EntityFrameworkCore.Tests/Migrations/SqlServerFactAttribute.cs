using System;
using Microsoft.Data.SqlClient;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Migrations;

/// <summary>
/// A test that needs a real SQL Server, and reports itself skipped when there is none.
/// <para>
/// Skipped rather than passing vacuously. A test that quietly returns when its
/// dependency is missing is the failure mode this repository has been cleaning out:
/// it shows a tick, and the tick means nothing. A skip shows a skip.
/// </para>
/// <para>
/// The server is <c>(localdb)\MSSQLLocalDB</c> by default — the same instance the
/// development host uses — and is overridable with the
/// <c>ASTROLABE_TEST_SQLSERVER</c> environment variable, which is how this would be
/// pointed at a container in CI.
/// </para>
/// </summary>
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (!SqlServerTestServer.IsAvailable)
        {
            Skip = "No SQL Server reachable at " + SqlServerTestServer.MasterConnectionString
                   + " — set ASTROLABE_TEST_SQLSERVER to point at one. "
                   + SqlServerTestServer.FailureReason;
        }
    }
}

/// <summary>Where the SQL Server tests connect, and whether anything answers.</summary>
public static class SqlServerTestServer
{
    private const string DefaultServer =
        @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True";

    public static string MasterConnectionString { get; } =
        Environment.GetEnvironmentVariable("ASTROLABE_TEST_SQLSERVER") ?? DefaultServer;

    public static string FailureReason { get; private set; } = string.Empty;

    /// <summary>
    /// Probed once. Discovery asks this for every attribute, and starting LocalDB
    /// takes a few seconds the first time.
    /// </summary>
    public static bool IsAvailable { get; } = Probe();

    /// <summary>A connection string for a throwaway database on the same server.</summary>
    public static string ForDatabase(string name) =>
        new SqlConnectionStringBuilder(MasterConnectionString) { InitialCatalog = name }.ConnectionString;

    private static bool Probe()
    {
        try
        {
            using var connection = new SqlConnection(
                new SqlConnectionStringBuilder(MasterConnectionString)
                {
                    InitialCatalog = "master",
                    ConnectTimeout = 30,
                }.ConnectionString);

            connection.Open();

            return true;
        }
        catch (Exception exception)
        {
            FailureReason = exception.GetType().Name + ": " + exception.Message;

            return false;
        }
    }
}
