# qdvc
Quick DVC, a faster alternative to DVC

### Quick Start

Copy the `qdvc.exe` from `H:\Transfer\PANAN\qdvc` to your Infinity root folder and run something like:

```
qdvc Data\Assets
```
to pull all the files from the `Data\Assets` folder that are tracked by DVC.

### Usage

```
qdvc <command> [-u <username>] [-p <password>] <path> [<path> ...]
```
`command` - should be one of: `pull`, `add`, `push`.
   - `pull` - downloads from the remote repository the files represented by the .dvc files given in the `path` argument.
     If the file exists locally, it will be overwritten.
   - `add` - adds the files given in the `path` argument to the repository by creating a .dvc file for each input file in the path arguments and copying the input files to the cache folder (if not already there).
   - `push` - uploads to the remote repository the files represented by the .dvc files given in the `path` argument.

`-u` - artifactory username

`-p` - artifactory access token

`<path> [<path> ...]` a collection of directories and files that will be pulled from DVC to their respective locations.
  Directories are processed recursively and all the files inside will be subject to the given command.


**NOTE**: 

The `-u` and `-p` parameters are optional. If any of them is not provided, QDVC will try to read them from the `.dvc\config.local` file relative to the first path provided.

If such file is not found or doen't contain the credentials, QDVC will try to read them from the `ARTIFACTORY_USERNAME` and `ARTIFACTORY_TOKEN` or `ARTIFACTORY_PASSWORD` environment variables.

### Remarks

QDVC is using the DVC's cache folder (.dvc\cache) and the same caching structure, so if the files where previously cached by a dvc command, QDVC will get them from this cache and not hit the remote repository.
If the files don't exist in the dvc cache, QDVC will download them to the cache folder and use them from there.

If either the `.dvc\config` or `.dvc\config.local` files contain a `[cache] dir` property, to alter the default cache location, QDVC will use that location instead of the default one.
  
----

### Building QDVC

Make sure you have [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed.

After checking out the source code, run:
  
```
dotnet restore
dotnet publish
```

This will produce the `qdvc.exe` in the `bin\Release\net8.0\win-x64\publish\` folder.
