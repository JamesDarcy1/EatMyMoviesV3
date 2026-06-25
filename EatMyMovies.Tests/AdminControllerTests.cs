using EatMyMoviesSite.Controllers;
using EatMyMoviesSite.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EatMyMovies.Tests;

public class AdminControllerTests
{
    [Fact]
    public void AdminController_RequiresAdminPolicyAndAntiforgeryByDefault()
    {
        var controllerType = typeof(AdminController);

        var authorize = Assert.Single(controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>());
        Assert.Equal(AdminAuthorization.PolicyName, authorize.Policy);
        Assert.NotEmpty(controllerType.GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), inherit: true));
    }

    [Fact]
    public void LoginActions_AllowAnonymous()
    {
        var loginActions = typeof(AdminController)
            .GetMethods()
            .Where(method => method.Name == nameof(AdminController.Login))
            .ToList();

        Assert.Equal(2, loginActions.Count);
        Assert.All(loginActions, action => Assert.NotEmpty(action.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true)));
    }

    [Fact]
    public void MovieOfTheWeekActions_DoNotAllowAnonymousAccess()
    {
        var actions = new[]
        {
            nameof(AdminController.MovieOfTheWeek),
            nameof(AdminController.SetMovieOfTheWeek),
            nameof(AdminController.ClearMovieOfTheWeek)
        };

        foreach (var actionName in actions)
        {
            var action = Assert.Single(typeof(AdminController).GetMethods(), method => method.Name == actionName);

            Assert.Empty(action.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));
        }
    }
}
