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
            context.SaveChanges();

            //Seed from Projects
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

                context.Projects.Add(project);
            }
            context.SaveChanges();

            //Seed from ProjectFloss
            var ProjectFlossLines = File.ReadAllLines(Path.Combine(folder, "ProjectFloss.csv"));
            foreach (var line in ProjectFlossLines)
            {
                var cells = line.Split(",");
                var projectFloss = new ProjectFloss()
                {
                    ProjectId = int.Parse(cells[0]),
                    FlossId = int.Parse(cells[1]),
                    Amount = int.Parse(cells[2])
                };

                context.ProjectFloss.Add(projectFloss);
            }
            context.SaveChanges();

            //Seed from Users
            var UserLines = File.ReadAllLines(Path.Combine(folder, "Users.csv")).Skip(1);
            foreach (var line in UserLines)
            {
                var cells = line.Split(",");
                var user = new User()
                {
                    Id = int.Parse(cells[0]),
                    Username = cells[1],
                    Password = cells[2],
                    Role = int.Parse(cells[3]),
                    CreatedOn = DateTime.Parse(cells[4]),
                    LastModified = DateTime.Parse(cells[5])
                };

                context.Users.Add(user);
            }
            context.SaveChanges();

            //Seed from UserProjects
            var UserProjectsLines = File.ReadAllLines(Path.Combine(folder, "UserProjects.csv"));
            foreach (var line in UserProjectsLines) 
            {
                var cells = line.Split(",");
                var UserProject = new UserProjects()
                {
                    UserId = int.Parse(cells[0]),
                    ProjectId = int.Parse(cells[1])
                };

                context.UserProjects.Add(UserProject);
            }
            context.SaveChanges();

            //Seed from UserFloss
            var UserFlossLines = File.ReadAllLines(Path.Combine(folder, "UserFloss.csv"));
            foreach (var line in UserFlossLines)
            {
                var cell = line.Split(",");
                var UserFloss = new UserFloss()
                {
                    UserId = int.Parse(cell[0]),
                    FlossId = int.Parse(cell[1]),
                    Amount = int.Parse(cell[2])
                };

                context.UserFloss.Add(UserFloss);
            }
            context.SaveChanges();
        }
    }
}