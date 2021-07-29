## Installation

```
dotnet add package Brighid.Identity.Client
```

## Security Recommendations

### Protecting the Client Secret

### In-Process Approach

1. Encrypt the client secret before passing it in as an environment variable.
2. When configuring Brighid Identity with `services.ConfigureBrighidIdentity<TConfig>(configuration)`, supply a class that extends IdentityConfig.  This will be wrapped in an `IOptions<TConfig>` and injected into the Service Container.
3. Have your Host pull the `IOptions<TConfig>` from the container on startup, decrypt the client secret, and re-assign it to the `TConfig.ClientSecret` property un-encrypted.  This can be done in an `IHostedService`.  If using [Lambdajection](https://github.com/cythral/lambdajection), this can be done automatically.

### Parent-Child Process / Layered Approach

1. Encrypt the client secret before passing it in as an environment variable.
2. Create a parent process that first decrypts that client secret and reassigns the environment variable with the decrypted text.
3. After decrypting the client secret, have the parent process start the child process (dotnet), which will inherit the decrypted environment variable.

With either approach, it is recommended to use your own configuration class that inherits from `IdentityConfig`.  This is because the options wrapper will be injected into the service container - and any third party service will be able to pull it and see the decrypted client secret.  By using your own IdentityConfig class, this makes it harder to figure out which `IOptions` service contains the client secret.