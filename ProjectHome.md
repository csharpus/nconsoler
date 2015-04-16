.Net Framework library for console arguments parsing

Project home page: http://nconsoler.csharpus.com

Quick example:

```
public static void Main(string[] args) {
    Consolery.Run(typeof(Program), args);
}

[Action]
public static void DoWork(
    [Required] string name,
    [Optional(-1)] int count) {
	// ...
}
```

```
C:\>program.exe Administrator /count:10
```


For more details use manual http://nconsoler.csharpus.com/manual