# qdvc
Quick DVC, a faster alternative to DVC

### Usage

```
qdvc -u <username> -p <password> <path> [<path> ...]
```

`-u` - artifactory username

`-p` - artifactory access token

`<path> [<path> ...]` a collection of directories and .dvc files that will be pulled from DVC to their respective locations.
  Directories are processed recursively and all the .dvc file inside them will be pulled.
  If the file exists locally, it will be overriden.

### Remarks

QDVC is using the DVC's cache folder (.dvc\cache) and the same caching structure, so if the files where previously cached by a dvc command, QDVC will get them from this cache and not hit the remote repository.
If the files don't exist in the dvc cache, QDVC will download them to the cache folder and use them from there.
  
For the moment, QDVC does only pull, the equivalent of the `dvc -R -f <paths>` command.
  
### Building QDVC

Make sure you have [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed.

After checking out the source code, run:
  
```
dotnet restore
dotnet publish
```

This will produce the `qdvc.exe` in the `bin\Release\net8.0\win-x64\publish\` folder.
