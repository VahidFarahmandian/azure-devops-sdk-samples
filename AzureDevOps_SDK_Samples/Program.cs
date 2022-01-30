using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System.Net;

#pragma warning disable CS8600 
Console.WriteLine("Enter Azure DevOps Server Uri:");
string url = Console.ReadLine();
Console.WriteLine("Enter username:");
string username = Console.ReadLine();
Console.WriteLine("Enter password:");
string password = "";

ConsoleKey key;
do
{
    var keyInfo = Console.ReadKey(intercept: true);
    key = keyInfo.Key;

    if (key == ConsoleKey.Backspace && password.Length > 0)
    {
        Console.Write("\b \b");
        password = password[0..^1];
    }
    else if (!char.IsControl(keyInfo.KeyChar))
    {
        Console.Write("*");
        password += keyInfo.KeyChar;
    }
} while (key != ConsoleKey.Enter);
#pragma warning restore CS8600 

//based on your requirement you might need to pass credentaials using PAT
VssCredentials creds = new WindowsCredential(new NetworkCredential(username, password));
VssConnection connection = new(new Uri(url ?? "https://dev.azure.com/"), creds);

foreach (var collection in await GetCollections(connection))
{
    Console.WriteLine($"Collection: {collection.Name}");
    connection = new VssConnection(new Uri(url + "/" + collection.Name), creds);
    try
    {
        foreach (var project in await GetProjects(connection))
        {
            Console.WriteLine($"    Project: {project.Name}");
            foreach (var repository in await GetRepositories(connection, project.Id))
            {
                Console.WriteLine($"    Repository: {repository.Name}");
                Console.WriteLine($"    Teams:");
                foreach (var team in await GetTeams(connection, project.Name))
                {
                    Console.WriteLine($"        Team: {team.Name}");
                    var members = await GetTeamMembers(connection, project.Name, team.Name);
                    if (members.Any() == false)
                        Console.WriteLine("         --- NO MEMBER ---");
                    foreach (var member in members)
                    {
                        Console.WriteLine("         " + member.Identity.DisplayName);
                    }
                }
            }
        }
    }
    catch (VssServiceException e)
    {
        Console.WriteLine(e.Message);
    }
}
Console.ReadLine();

static async Task<IOrderedEnumerable<TeamProjectCollectionReference>> GetCollections(VssConnection connection)
{
    ProjectCollectionHttpClient pchc = connection.GetClient<ProjectCollectionHttpClient>();
    return (await pchc.GetProjectCollections()).OrderBy(x => x.Name);
}

static async Task<IOrderedEnumerable<TeamProjectReference>> GetProjects(VssConnection connection)
{
    ProjectHttpClient phc = connection.GetClient<ProjectHttpClient>();
    return (await phc.GetProjects()).OrderBy(x => x.Name);
}

static async Task<IOrderedEnumerable<GitRepository>> GetRepositories(VssConnection connection, Guid project)
{
    GitHttpClient ghc = connection.GetClient<GitHttpClient>();
    return (await ghc.GetRepositoriesAsync(project)).OrderBy(x => x.Name);
}

static async Task<IOrderedEnumerable<WebApiTeam>> GetTeams(VssConnection connection, string project)
{
    TeamHttpClient thc = connection.GetClient<TeamHttpClient>();
    return (await thc.GetTeamsAsync(project)).OrderBy(x => x.Name);
}

static async Task<IOrderedEnumerable<TeamMember>> GetTeamMembers(VssConnection connection, string project, string team)
{
    TeamHttpClient thc = connection.GetClient<TeamHttpClient>();
    return (await thc.GetTeamMembersWithExtendedPropertiesAsync(project, team)).OrderBy(x => x.Identity.DisplayName);
}