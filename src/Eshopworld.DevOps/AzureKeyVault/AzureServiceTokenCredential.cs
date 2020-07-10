using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Azure.Services.AppAuthentication;

namespace Eshopworld.DevOps.AzureKeyVault
{
    internal class AzureServiceTokenCredential : TokenCredential
    {
        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            var token = await new AzureServiceTokenProvider().GetAccessTokenAsync("https://vault.azure.net", string.Empty).ConfigureAwait(false);
            return new AccessToken(token, DateTimeOffset.UtcNow.AddMinutes(5.0));
        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return GetTokenAsync(requestContext, cancellationToken).Result;
        }
    }
}
