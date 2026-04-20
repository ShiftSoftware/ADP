using Blazored.LocalStorage;
using Microsoft.Extensions.Configuration;
using ShiftSoftware.ShiftIdentity.Blazor;
using ShiftSoftware.ShiftIdentity.Blazor.Services;
using ShiftSoftware.ShiftIdentity.Core.DTOs;

namespace ShiftSoftware.ADP.Surveys.Web.WebServices;

/// <summary>
/// Consumer-side <see cref="IIdentityStore"/> implementation — stores the access token
/// in Blazored LocalStorage and the refresh token in a cookie. Mirrors the Menus
/// precedent (<c>ADP.Menus.Web.WebServices.TokenStorageService</c>).
/// </summary>
public class TokenStorageService : IIdentityStore
{
    private readonly CookieService cookieService;
    private readonly IConfiguration configuration;
    private readonly ILocalStorageService localStorage;
    private readonly ISyncLocalStorageService syncLocalStorage;
    private const string tokenStorageKey = "token";
    private const string refreshTokenStorageKey = "refresh-token";

    public TokenStorageService(
        CookieService cookieService,
        IConfiguration configuration,
        ILocalStorageService localStorage,
        ISyncLocalStorageService syncLocalStorage)
    {
        this.cookieService = cookieService;
        this.configuration = configuration;
        this.localStorage = localStorage;
        this.syncLocalStorage = syncLocalStorage;
    }

    public async Task StoreTokenAsync(TokenDTO tokenDto)
    {
        var domain = configuration.GetValue<string>("CookieDomain");
        await cookieService.SetItemAsStringAsync(refreshTokenStorageKey, tokenDto.RefreshToken, domain, "/",
            (int)tokenDto.RefreshTokenLifeTimeInSeconds.GetValueOrDefault());

        await localStorage.SetItemAsync(tokenStorageKey, tokenDto);
    }

    public async Task RemoveTokenAsync()
    {
        var domain = configuration.GetValue<string>("CookieDomain");
        await cookieService.RemoveItemAsync(refreshTokenStorageKey, domain, "/");

        await localStorage.RemoveItemAsync(tokenStorageKey);
    }

    public string? GetToken()
    {
        return syncLocalStorage.GetItem<TokenDTO>(tokenStorageKey)?.Token;
    }

    public async Task<TokenDTO?> GetTokenAsync()
    {
        var tokenDto = await localStorage.GetItemAsync<TokenDTO>(tokenStorageKey);
        var refreshToken = await cookieService.GetItemAsStringAsync(refreshTokenStorageKey);

        tokenDto ??= new TokenDTO();
        tokenDto.RefreshToken = refreshToken!;

        return tokenDto;
    }
}
