using Microsoft.AspNetCore.Identity;
namespace SprintPilot.Api;
public static class Seed
{
    public static async Task Run(SprintDb db, IPasswordHasher<User> hasher)
    {
        User demo = new()
        {
            Email = "demo@sprintpilot.local"
        }, second = new()
        {
            Email = "second@sprintpilot.local"
        };
        foreach (User u in new[] { demo, second })
        {
            u.PasswordHash = hasher.HashPassword(u, "DemoPass123!");
            db.Users.Add(u);
        }
        string[] titles = ["Let customers find their next appointment", "Send a reminder before an appointment", "Build the appointment booking flow", "Make rescheduling feel effortless", "Keep appointment details in sync", "Show a clear booking confirmation", "Set up the customer profile", "Define the appointment data contract"];
        string[] descriptions = ["As a customer, I want to browse available appointment slots so I can choose a time that fits my schedule.", "As a customer, I want a reminder before my appointment so I can plan ahead.", "As a customer, I want to book an available slot so I can reserve time with a service provider.", "As a customer, I want to move an existing booking so I can adapt to a change in plans.", "As a customer, I want my appointment details to stay consistent across views so I can trust the information.", "As a customer, I want a confirmation after booking so I know my appointment is reserved.", "As a customer, I want to manage my profile so my contact information stays up to date.", "As a developer, I want an agreed appointment contract so the client and API exchange consistent data."];
        string[] states = ["Backlog", "Backlog", "In progress", "In progress", "In review", "In review", "Done", "Done"];
        for (int n = 0; n < titles.Length; n++)
        {
            StoryContent c = MockPlanner.Template(titles[n], descriptions[n]) with
            {
                Description = descriptions[n],
                Priority = n == 2 ? "High" : n == 7 ? "Low" : "Medium",
                Points = n is 0 or 2 ? 5 : 3
            };
            if (n == 2)
                c = c with
                {
                    Acceptance = ["An available slot can be reserved by an authenticated customer.", "A slot already booked by another customer cannot be reserved again.", "The customer sees a confirmation with the selected date and time."],
                    Tasks = ["Add a booking endpoint with an atomic slot reservation.", "Build date, time and provider selection.", "Verify duplicate-submission and concurrent booking behavior."]
                };
            Story s = new()
            {
                OwnerId = demo.Id,
                ContentJson = Json.Write(c),
                Status = states[n],
                Assignee = n is 2 or 4 ? "Alex Morgan" : n is 3 or 5 ? "Jamie Chen" : "",
                Blocked = n == 3,
                Blocker = n == 3 ? "Waiting for the cancellation and rescheduling policy." : "",
                CreatedAt = DateTime.UtcNow.AddDays(-7 + n)
            };
            s.Activity.Add(new Activity { Text = "Fictional demo story created. Statuses are seeded; no test execution is implied.", At = s.CreatedAt });
            db.Stories.Add(s);
        }
        await db.SaveChangesAsync();
    }
}
