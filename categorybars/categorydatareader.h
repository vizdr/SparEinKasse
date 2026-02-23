#pragma once

#include <QObject>
#include <QVariantList>
#include <QStringList>

class CategoryDataReader : public QObject
{
    Q_OBJECT
    Q_PROPERTY(QVariantList timespans READ timespans NOTIFY dataLoaded)
    Q_PROPERTY(QStringList allCategories READ allCategories NOTIFY dataLoaded)
    Q_PROPERTY(qreal maxExpense READ maxExpense NOTIFY dataLoaded)
    Q_PROPERTY(bool hasError READ hasError NOTIFY dataLoaded)
    Q_PROPERTY(QString errorString READ errorString NOTIFY dataLoaded)

public:
    explicit CategoryDataReader(QObject *parent = nullptr);

    Q_INVOKABLE bool loadFromFile(const QString &path);

    QVariantList timespans() const { return m_timespans; }
    QStringList allCategories() const { return m_allCategories; }
    qreal maxExpense() const { return m_maxExpense; }
    bool hasError() const { return m_hasError; }
    QString errorString() const { return m_errorString; }

signals:
    void dataLoaded();

private:
    QVariantList m_timespans;
    QStringList m_allCategories;
    qreal m_maxExpense = 0.0;
    bool m_hasError = false;
    QString m_errorString;
};
