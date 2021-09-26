shimexe
=======

Quickly add executables to your path.

### How to install
With [Scoop](http://scoop.sh), run:
```
scoop install https://raw.githubusercontent.com/lukesampson/shimexe/master/scoop/shim.json
```

### How to build from source..

Run `src/build.ps1`. This creates `lib/shim.exe`, which is the .NET executable used by `bin/shim.ps1` when creating shims.


### To create shims...

If you haven't installed with [Scoop](http://scoop.sh), you can create shims using `bin/shim.ps1`. Run without arguments for help.

### fork to change list

> this is fork to [scoop/shimexe](https://github.com/lukesampson/shimexe)

- add `dir` field to `.shim` config, change to open pwd
- add `environment` field to `.shim` config, change parent environment: `TZ = Pacific/Pitcairn;CHERE_INVOKING=1`
- add `signal` field signal capture support shim parent process not exit: `signal = true`
