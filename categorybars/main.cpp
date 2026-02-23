#include <QGuiApplication>
#include <QQuickView>
#include <QQmlContext>
#include <QQmlEngine>
#include <QUrl>

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

    CategoryDataReader dataReader;
    if (!jsonPath.isEmpty())
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
    view.resize(1024, 850);
    view.show();

    return app.exec();
}
