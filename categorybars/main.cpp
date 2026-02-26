#include <QGuiApplication>
#include <QQuickView>
#include <QQmlContext>
#include <QQmlEngine>
#include <QUrl>
#include <QFileInfo>

#include "categorydatareader.h"

int main(int argc, char *argv[])
{
    QGuiApplication app(argc, argv);
    app.setApplicationName(QStringLiteral("Category-Expense 3D"));

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

    CategoryDataReader dataReader;
    dataReader.loadFromFile(jsonPath);

    QQuickView view;
    view.setResizeMode(QQuickView::SizeRootObjectToView);
    // Grau setzen (Mittelgrau)
    view.setColor(QColor(QStringLiteral("#808080")));

    // Add the app binary directory so windeployqt-copied QML modules are found
    view.engine()->addImportPath(QGuiApplication::applicationDirPath());
    view.rootContext()->setContextProperty(QStringLiteral("dataReader"), &dataReader);

    QObject::connect(view.engine(), &QQmlEngine::quit, &app, &QGuiApplication::quit);

    view.setSource(QUrl(QStringLiteral("qrc:/CategoryBarsExample/qml/categorybars/main.qml")));
    view.setTitle(QStringLiteral("Category-Expense Comparison"));
    view.resize(1024, 800);
    view.show();

    return app.exec();
}
