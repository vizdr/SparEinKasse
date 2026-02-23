#include "categorydatareader.h"

#include <QFile>
#include <QJsonDocument>
#include <QJsonObject>
#include <QJsonArray>
#include <QJsonValue>

CategoryDataReader::CategoryDataReader(QObject *parent)
    : QObject(parent)
{
}

bool CategoryDataReader::loadFromFile(const QString &path)
{
    m_timespans.clear();
    m_allCategories.clear();
    m_maxExpense = 0.0;
    m_hasError = false;
    m_errorString.clear();

    QFile file(path);
    if (!file.open(QIODevice::ReadOnly)) {
        m_hasError = true;
        m_errorString = QStringLiteral("Cannot open file: ") + path;
        emit dataLoaded();
        return false;
    }

    QJsonParseError parseError;
    QJsonDocument doc = QJsonDocument::fromJson(file.readAll(), &parseError);
    file.close();

    if (parseError.error != QJsonParseError::NoError) {
        m_hasError = true;
        m_errorString = QStringLiteral("JSON parse error: ") + parseError.errorString();
        emit dataLoaded();
        return false;
    }

    if (!doc.isObject()) {
        m_hasError = true;
        m_errorString = QStringLiteral("JSON root must be an object");
        emit dataLoaded();
        return false;
    }

    QJsonArray timespansArray = doc.object().value(QStringLiteral("timespans")).toArray();
    QStringList categoryOrder; // preserve first-seen order

    for (const QJsonValue &tsVal : timespansArray) {
        QJsonObject tsObj = tsVal.toObject();
        QVariantMap tsMap;
        tsMap[QStringLiteral("label")]     = tsObj.value(QStringLiteral("label")).toString();
        tsMap[QStringLiteral("beginDate")] = tsObj.value(QStringLiteral("beginDate")).toString();
        tsMap[QStringLiteral("endDate")]   = tsObj.value(QStringLiteral("endDate")).toString();

        QVariantList categoriesList;
        QJsonArray catsArray = tsObj.value(QStringLiteral("categories")).toArray();
        for (const QJsonValue &catVal : catsArray) {
            QJsonObject catObj = catVal.toObject();
            QString name = catObj.value(QStringLiteral("name")).toString();
            qreal expense = catObj.value(QStringLiteral("expense")).toDouble();

            QVariantMap catMap;
            catMap[QStringLiteral("name")]    = name;
            catMap[QStringLiteral("expense")] = expense;
            categoriesList.append(catMap);

            if (!categoryOrder.contains(name))
                categoryOrder.append(name);
            if (expense > m_maxExpense)
                m_maxExpense = expense;
        }
        tsMap[QStringLiteral("categories")] = categoriesList;
        m_timespans.append(tsMap);
    }

    m_allCategories = categoryOrder;
    emit dataLoaded();
    return true;
}
