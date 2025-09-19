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
            if (context.Projects.Any()) return;

            string folder = Path.Combine(AppContext.BaseDirectory, "SeedData");

            //Seed from Floss
            var FlossLines = File.ReadAllLines(Path.Combine(folder, "Floss.csv")).Skip(1);
            foreach (var line in FlossLines)
            {
                var cells = line.Split(',');
                var floss = new Floss()
                {
                    Id = int.Parse(cells[0]),
                    Name = cells[1],
                    Number = cells[2],
                    HexColor = cells[3],
                    CreatedOn = DateTime.Parse(cells[4]),
                    LastModified = DateTime.Parse(cells[5])
                };

                context.Floss.Add(floss);
            }
            var FlossLookup = context.Floss.ToDictionary(f => f.Id);

            //Seed from Projects
            List<Project> Project = new List<Project>();
            var ProjectLines = File.ReadAllLines(Path.Combine(folder, "Projects.csv")).Skip(1);
            foreach (var line in ProjectLines)
            {
                var cells = line.Split(',');
                var project = new Project()
                {
                    Id = int.Parse(cells[0]),
                    Name = cells[1],
                    FileName = cells[2],
                    IsCompleted = cells[3] == "0",
                    CreatedOn = DateTime.Parse(cells[5]),
                    LastModified = DateTime.Parse(cells[6])
                };
                if (project.IsCompleted) project.CompletionDate = DateTime.Parse(cells[4]);
                else project.CompletionDate = null;

                //Find related flosses from projectfloss.csv
                var ProjectFloss = File.ReadAllLines(Path.Combine(folder, "ProjectFloss.csv"))
                    .Skip(1)
                    .Where(p => p.Split(',')[0] == project.Id.ToString());
                foreach (var pf in ProjectFloss)
                {
                    var pfcells = pf.Split(",");
                    int flossId = int.Parse(pfcells[1]);
                    int flossAmount = int.Parse(pfcells[2]);
                    if (FlossLookup.TryGetValue(flossId, out var flossToAdd))
                    {
                        project.Floss.Add(flossToAdd, flossAmount);
                    }
                }

                context.Projects.Add(project);
            }
            var ProjectLookup = context.Projects.ToDictionary(p => p.Id);

            //Seed from Users
            List<User> Users = new List<User>();
            var UserLines = File.ReadAllLines(Path.Combine(folder, "Users.csv")).Skip(1);
            foreach (var line in UserLines)
            {
                var cells = line.Split(",");
                var user = new User()
                {
                    Id = int.Parse(cells[0]),
                    Username = cells[1],
                    Password = cells[2],
                    CreatedOn = DateTime.Parse(cells[3]),
                    LastModified = DateTime.Parse(cells[4])
                };

                //Find related flosses from userfloss.csv
                var UserFloss = File.ReadAllLines(Path.Combine(folder, "UserFloss.csv"))
                    .Skip(1)
                    .Where(u => u.Split(",")[0] == user.Id.ToString());
                foreach (var uf in UserFloss)
                {
                    var ufcells = uf.Split(",");
                    int flossId = int.Parse(ufcells[1]);
                    int flossAmount = int.Parse(ufcells[2]);
                    if (FlossLookup.TryGetValue(flossId, out var flossToAdd))
                    {
                        user.Floss.Add(flossToAdd, flossAmount);
                    }
                }

                //Find related projects from userProjects.csv
                var UserProjects = File.ReadAllLines(Path.Combine(folder, "UserProjects.csv"))
                    .Skip(1)
                    .Where(u => u.Split(",")[0] == user.Id.ToString());
                foreach (var up in UserProjects)
                {
                    var upcells = up.Split(",");
                    int projectId = int.Parse(upcells[1]);
                    if (ProjectLookup.TryGetValue(projectId, out var projectToAdd))
                    {
                        user.Projects.Add(projectToAdd);
                    }
                }

                context.Users.Add(user);
            }

            context.SaveChanges();
        }
    }
}