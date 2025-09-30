using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Server.Data;
using Server.Models;
using System;
using System.Formats.Asn1;
using System.Globalization;
using System.Linq;

public static class SeedData
{
    public static void Initialize(IServiceProvider serviceProvider)
    {
        using (var context = new ThreadfolioContext(serviceProvider.GetRequiredService<DbContextOptions<ThreadfolioContext>>()))
        {
            //check for data already there
            if (context.UserFloss.Any()) return;

            string folder = Path.Combine(AppContext.BaseDirectory, "..", "..", "..");
            folder = Path.Combine(folder, "Storage", "SeedData");

            //Seed from Floss
            var FlossLines = File.ReadAllLines(Path.Combine(folder, "Floss.csv"));
            var FlossHeaders = FlossLines[0].Split(',');
            foreach (var line in FlossLines.Skip(1))
            {
                var cells = line.Split(',');
                var row = FlossHeaders.Zip(cells, (h, c) => new {Header = h, Value = c}).ToDictionary(x => x.Header, x => x.Value);
                var floss = new Floss()
                {
                    Name = row["Name"],
                    Number = row["Number"],
                    HexColor = row["HexColor"],
                    CreatedOn = DateTime.Parse(row["CreatedOn"]),
                    LastModified = DateTime.Parse(row["LastModified"])
                };

                context.Floss.Add(floss);
            }
            context.SaveChanges();

            //Seed from Projects
            var ProjectLines = File.ReadAllLines(Path.Combine(folder, "Projects.csv"));
            var ProjectHeaders = ProjectLines[0].Split(',');
            foreach (var line in ProjectLines.Skip(1))
            {
                var cells = line.Split(',');
                var row = ProjectHeaders.Zip(cells, (h, c) => new { Header = h, Value = c }).ToDictionary(x => x.Header, x => x.Value);
                var project = new Project()
                {
                    Name = row["Name"],
                    FileName = row["FileName"],
                    IsCompleted = row["Completed"] == "1",
                    CreatedOn = DateTime.Parse(row["CreatedOn"]),
                    LastModified = DateTime.Parse(row["LastModified"])
                };
                if (project.IsCompleted) project.CompletionDate = DateTime.Parse(row["CompletionDate"]);
                else project.CompletionDate = null;

                context.Projects.Add(project);
            }
            context.SaveChanges();

            //Seed from ProjectFloss
            var ProjectFlossLines = File.ReadAllLines(Path.Combine(folder, "ProjectFloss.csv"));
            var PFHeaders = ProjectFlossLines[0].Split(",");
            foreach (var line in ProjectFlossLines.Skip(1))
            {
                var cells = line.Split(",");
                var row = PFHeaders.Zip(cells, (h, c) => new { Header = h, Value = c }).ToDictionary(x => x.Header, x => x.Value);
                var projectFloss = new ProjectFloss()
                {
                    ProjectId = int.Parse(row["ProjectId"]),
                    FlossId = int.Parse(row["FlossId"]),
                    Amount = int.Parse(row["Amount"])
                };

                context.ProjectFloss.Add(projectFloss);
            }
            context.SaveChanges();

            //Seed from Users
            var UserLines = File.ReadAllLines(Path.Combine(folder, "Users.csv"));
            var UserHeaders = UserLines[0].Split(',');
            foreach (var line in UserLines.Skip(1))
            {
                var cells = line.Split(",");
                var row = UserHeaders.Zip(cells, (h, c) => new { Header = h, Value = c }).ToDictionary(x => x.Header, x => x.Value);

                UserType type;
                var cell = row["Role"];
                if (cell == "1") type = UserType.Admin;
                else if (cell == "2") type = UserType.User;
                else type = UserType.Anon;

                var user = new User()
                {
                    Username = row["Username"],
                    Password = row["Password"],
                    Role = type,
                    CreatedOn = DateTime.Parse(row["CreatedOn"]),
                    LastModified = DateTime.Parse(row["LastModified"])
                };

                context.Users.Add(user);
            }
            context.SaveChanges();

            //Seed from UserProjects
            var UserProjectsLines = File.ReadAllLines(Path.Combine(folder, "UserProjects.csv"));
            var UPHeaders = UserProjectsLines[0].Split(",");
            foreach (var line in UserProjectsLines.Skip(1)) 
            {
                var cells = line.Split(",");
                var row = UPHeaders.Zip(cells, (h, c) => new { Header = h, Value = c }).ToDictionary(x => x.Header, x => x.Value);
                var UserProject = new UserProjects()
                {
                    UserId = int.Parse(row["UserId"]),
                    ProjectId = int.Parse(row["ProjectId"])
                };

                context.UserProjects.Add(UserProject);
            }
            context.SaveChanges();

            //Seed from UserFloss
            var UserFlossLines = File.ReadAllLines(Path.Combine(folder, "UserFloss.csv"));
            var UFHeaders = UserFlossLines[0].Split(',');
            foreach (var line in UserFlossLines.Skip(1))
            {
                var cells = line.Split(",");
                var row = UFHeaders.Zip(cells, (h, c) => new { Header = h, Value = c }).ToDictionary(x => x.Header, x => x.Value);
                var UserFloss = new UserFloss()
                {
                    UserId = int.Parse(row["UserId"]),
                    FlossId = int.Parse(row["FlossId"]),
                    Amount = int.Parse(row["Amount"])
                };

                context.UserFloss.Add(UserFloss);
            }
            context.SaveChanges();
        }
    }
}