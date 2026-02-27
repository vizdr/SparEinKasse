#include <QGuiApplication>
#include <QQuickView>
#include <QQmlContext>
#include <QQmlEngine>
#include <QUrl>
#include <QFileInfo>
#include <QDir>
#include <QDateTime>
#include <QFile>

#include "categorydatareader.h"

// ── Diagnostic file logger ──────────────────────────────────────────────────
static QFile* g_logFile = nullptr;

static void msgHandler(QtMsgType type, const QMessageLogContext&, const QString& msg)
{
    if (!g_logFile) return;
    QByteArray line;
    line += QDateTime::currentDateTime().toString("hh:mm:ss.zzz").toUtf8();
    line += ' ';
    switch (type) {
    case QtCriticalMsg: line += "[CRIT]  "; break;
    case QtWarningMsg:  line += "[WARN]  "; break;
    case QtFatalMsg:    line += "[FATAL] "; break;
    default:            line += "[info]  "; break;
    }
    line += msg.toUtf8();
    line += '\n';
    g_logFile->write(line);
    g_logFile->flush();
}
// ───────────────────────────────────────────────────────────────────────────

int main(int argc, char *argv[])
{
    // Open log file in the system temp directory (always writable)
    QFile logFile(QDir::tempPath() + "/categorybars_error.log");
    if (logFile.open(QIODevice::WriteOnly | QIODevice::Text | QIODevice::Truncate))
    {
        g_logFile = &logFile;
        qInstallMessageHandler(msgHandler);
    }

    QGuiApplication app(argc, argv);
    app.setApplicationName(QStringLiteral("Category-Expense 3D"));

    qInfo() << "=== categorybars startup ===";
    qInfo() << "App dir:  " << QGuiApplication::applicationDirPath();
    qInfo() << "Temp dir: " << QDir::tempPath();

    QString jsonPath;
    if (argc > 1)
    {
        jsonPath = QString::fromLocal8Bit(argv[1]);
    }
    // Resolve relative paths against the executable directory, not the CWD
    if (!jsonPath.isEmpty())
    {
        QFileInfo fi(jsonPath);
        if (fi.isRelative())
            jsonPath = QGuiApplication::applicationDirPath() + "/" + jsonPath;
    }
    else
    {
        jsonPath = QGuiApplication::applicationDirPath() + "/test_sample.json";
    }

    qInfo() << "JSON path:" << jsonPath;
    qInfo() << "JSON exists:" << QFileInfo::exists(jsonPath);

    CategoryDataReader dataReader;
    dataReader.loadFromFile(jsonPath);

    QQuickView view;
    view.setResizeMode(QQuickView::SizeRootObjectToView);
    // Grau setzen (Mittelgrau)
    view.setColor(QColor(QStringLiteral("#808080")));

    // Add the qml/ subdirectory where windeployqt6 deploys QML modules
    QString qmlImportPath = QGuiApplication::applicationDirPath() + "/qml";
    qInfo() << "QML import path:" << qmlImportPath;
    qInfo() << "QML dir exists:" << QDir(qmlImportPath).exists();
    view.engine()->addImportPath(qmlImportPath);
    view.rootContext()->setContextProperty(QStringLiteral("dataReader"), &dataReader);

    QObject::connect(view.engine(), &QQmlEngine::quit, &app, &QGuiApplication::quit);

    // Log QML loading result (errors appear here when an import fails)
    QObject::connect(&view, &QQuickView::statusChanged,
                     [&view](QQuickView::Status status)
    {
        switch (status) {
        case QQuickView::Loading:
            qInfo() << "QML status: Loading";
            break;
        case QQuickView::Ready:
            qInfo() << "QML status: Ready — UI loaded successfully";
            break;
        case QQuickView::Null:
            qInfo() << "QML status: Null (no source set)";
            break;
        case QQuickView::Error:
            qCritical() << "QML status: Error — content will not be displayed";
            for (const auto& err : view.errors())
                qCritical() << "  " << err.toString();
            break;
        }
    });

    view.setSource(QUrl(QStringLiteral("qrc:/CategoryBarsExample/qml/categorybars/main.qml")));
    view.setTitle(QStringLiteral("Category-Expense Comparison"));
    view.resize(1024, 850);
    view.show();

    qInfo() << "Entering event loop";
    return app.exec();
}
