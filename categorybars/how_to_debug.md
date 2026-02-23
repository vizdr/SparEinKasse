# How to Debug `categorybars` in Visual Studio 2022

## Step 1 — Open the solution

Open `build/categorybars.sln` in VS 2022.

Right-click `categorybars` in Solution Explorer → **Set as Startup Project**.

---

## Step 2 — Fix the console window (critical)

`WIN32_EXECUTABLE TRUE` in `CMakeLists.txt` suppresses the console, making `qDebug()` and QML errors invisible. For debugging, comment it out:

```cmake
# set_target_properties(categorybars PROPERTIES
#     WIN32_EXECUTABLE TRUE
#     MACOSX_BUNDLE TRUE
# )
```

Then rebuild: either `cmake --build build` from the project folder, or **Ctrl+Shift+B** in VS.

Now `qDebug()` and QML errors appear in the VS **Output** pane (Debug channel), and in a console window when run outside VS.

---

## Step 3 — Pass the JSON argument

The app requires `argv[1]` = path to the JSON file.

**Project → Properties → Debugging → Command Arguments:**
```
C:\Users\Vladimir\Documents\SSKA_Analizer_Source-1.3-1.4\categorybars\test_sample.json
```

`test_sample.json` is already in the project root and can be used for testing.

---

## Step 4 — Debug C++ code

- Set breakpoints in `.cpp` / `.h` files normally
- Press **F5** to start with the debugger attached
- Inspect variables (`dataReader`, `jsonPath`, etc.) in Locals / Watch windows

---

## Step 5 — Debug QML errors

QML errors appear in **Output → Debug**. Look for lines like:

```
qml: ...
file:///...:42: TypeError: Cannot read property of null
```

For richer QML debugging, install **Qt Visual Studio Tools**:
- **Extensions → Manage Extensions** → search "Qt Visual Studio Tools" (publisher: Qt Group)
- Adds a QML debugger, Qt object inspector, and property viewer

---

## Step 6 — Capture QML errors to a file (alternative)

If Output pane messages are hard to capture, add a temporary file-based message handler in `main.cpp`:

```cpp
#include <QFile>
#include <QTextStream>

void myMsgHandler(QtMsgType, const QMessageLogContext &, const QString &msg) {
    QFile f("C:/Users/Vladimir/categorybars_debug.txt");
    f.open(QIODevice::Append | QIODevice::Text);
    QTextStream(&f) << msg << "\n";
}

// Call before QGuiApplication app(...):
qInstallMessageHandler(myMsgHandler);
```

Remove this handler before shipping / re-enabling `WIN32_EXECUTABLE`.

---

## Quick reference

| Symptom | Fix |
|---|---|
| No console / Output text | Comment out `WIN32_EXECUTABLE TRUE` and rebuild |
| App crashes on start | Check Output pane for QML load errors |
| "module not found" QML error | Verify `view.engine()->addImportPath(QGuiApplication::applicationDirPath())` is present in `main.cpp` |
| No data displayed | Check command argument path; validate JSON format |
| QML breakpoints not hitting | Install Qt Visual Studio Tools extension |

---

## Debugging when launched from WPF

The WPF app launches `categorybars.exe` as a separate process via `Process.Start`. Since these are different runtimes (C#/.NET vs C++/Qt), you need to attach the C++ debugger separately.

### Option A — Attach to Process (simplest, no code changes)

1. Build `categorybars` in **Debug** config (comment out `WIN32_EXECUTABLE TRUE` first)
2. Run the WPF app → click "Category-Expense 3D" → `categorybars.exe` starts
3. In VS 2022: **Debug → Attach to Process...** (`Ctrl+Alt+P`)
4. Find `categorybars.exe` → connection type **Native** → **Attach**

> Have the Attach dialog open **before** clicking the WPF button — the window to react is small.

### Option B — Add a startup delay (comfortable attach window)

Add a temporary sleep at the top of `main()` so you have time to attach:

```cpp
#include <thread>
#include <chrono>
// first line inside main():
std::this_thread::sleep_for(std::chrono::seconds(10));
```

Rebuild, click the WPF button, attach within 10 s, then the app continues. Remove the sleep when done.

### Option C — Two VS 2022 instances

- **Instance 1**: `WpfApp-SSKA.sln` — run WPF with F5
- **Instance 2**: `categorybars.sln` — use **Attach to Process** after WPF launches it

Both debuggers work independently: C# in one, C++ in the other.

### Option D — Launch directly with the saved JSON (fastest iteration)

The WPF always writes the JSON to the same temp path:
```
%TEMP%\sska_category_expense_3d.json
```

1. Click the WPF button once to generate the JSON
2. In `categorybars` VS project: **Project → Properties → Debugging → Command Arguments:**
   ```
   C:\Users\Vladimir\AppData\Local\Temp\sska_category_expense_3d.json
   ```
3. Press **F5** — no WPF needed. The JSON is stable between runs.

This is the **fastest loop** for iterating on Qt/QML logic.

### Which option to use

| Goal | Best option |
|---|---|
| Quick one-off C++ breakpoint | A — Attach to Process |
| Comfortable attach window | B — Sleep in main() |
| Debug both WPF and Qt simultaneously | C — Two VS instances |
| Iterate on QML/data logic fast | D — Launch directly with saved JSON |
