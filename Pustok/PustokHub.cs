using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Pustok.Models;

namespace Pustok
{
    public class PustokHub:Hub
    {
        private readonly UserManager<AppUser> _userManager;

        public PustokHub(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }
        public override Task OnConnectedAsync()
        {
            if(Context.User.Identity.IsAuthenticated && Context.User.IsInRole("Member"))
            {
                AppUser user = _userManager.FindByNameAsync(Context.User.Identity.Name).Result;
                user.IsOnline = true;
                user.ConnectionId = Context.ConnectionId;

                var result = _userManager.UpdateAsync(user).Result;
                Clients.All.SendAsync("Connected", user.Id);
            }
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            if (Context.User.Identity.IsAuthenticated && Context.User.IsInRole("Member"))
            {
                AppUser user = _userManager.FindByNameAsync(Context.User.Identity.Name).Result;
                user.IsOnline = false;
                user.LastOnlineAt = DateTime.UtcNow.AddHours(4);

                var result = _userManager.UpdateAsync(user).Result;
                Clients.All.SendAsync("Disconnected", user.Id);

            }
            return base.OnDisconnectedAsync(exception);
        }
    }
}
