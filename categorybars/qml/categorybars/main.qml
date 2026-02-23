import QtQuick
import QtQuick.Controls.Basic
import QtGraphs

Item {
    id: root
    width: 1024
    height: 850

    ListModel {
        id: flatModel
    }

    Rectangle {
        anchors.fill: parent
        //color: "#1a1a2e"
        color: "#808080"
    }

    Rectangle {
        id: closeBtn
        z: 100
        anchors { top: parent.top; right: parent.right; margins: 8 }
        width: 110; height: 32
        color: closeMouse.containsMouse ? "#c0392b" : "#922b21"
        radius: 4

        Text {
            anchors.centerIn: parent
            text: "Return and close"
            color: "white"
            font.bold: true
            font.pixelSize: 13
        }

        MouseArea {
            id: closeMouse
            anchors.fill: parent
            hoverEnabled: true
            onClicked: Qt.quit()
        }
    }

    Text {
        id: titleText
        text: "Category-Expense Comparison"
        color: "#e0e0e0"
        font { pixelSize: 20; bold: true }
        anchors { top: parent.top; horizontalCenter: parent.horizontalCenter; topMargin: 10 }
        z: 10
    }

    Row {
        id: rotationRow
        anchors {
            top: titleText.bottom
            horizontalCenter: parent.horizontalCenter
            topMargin: 10
        }
        spacing: 8

        Text {
            text: "Rotation:"
            color: "#e0e0e0"
            font.pixelSize: 13
            anchors.verticalCenter: parent.verticalCenter
        }

        Slider {
            id: rotationSlider
            from: -180
            to: 180
            value: -20
            width: 400
            anchors.verticalCenter: parent.verticalCenter
        }

        Text {
            text: Math.round(rotationSlider.value) + "°"
            color: "#e0e0e0"
            font.pixelSize: 13
            width: 36
            anchors.verticalCenter: parent.verticalCenter
        }
    }

    Bars3D {
        id: chart
        anchors {
            top: rotationRow.bottom
            left: parent.left
            right: parent.right
            bottom: parent.bottom
            topMargin: 4
            bottomMargin: 15
        }

        theme: GraphsTheme {
            theme: GraphsTheme.PurpleSeries
            colorScheme: GraphsTheme.ColorScheme.Dark
            plotAreaBackgroundVisible: true
            colorStyle: GraphsTheme.ColorStyle.RangeGradient
            baseGradients: [
                Gradient {
                    GradientStop { position: 0.0; color: "#4A148C" }
                    GradientStop { position: 1.0; color: "#CE93D8" }
                }
            ]
        }

        shadowQuality: Graphs3D.ShadowQuality.Medium
        selectionMode: Graphs3D.SelectionFlag.Item
        cameraPreset: Graphs3D.CameraPreset.IsometricRightHigh
        cameraXRotation: rotationSlider.value
        cameraYRotation: 42
        cameraZoomLevel: 115
        margin: 0.0
        aspectRatio: 2.5

        rowAxis: Category3DAxis {
            title: "Time Span"
            titleVisible: false
            titleOffset: -0.9
            titleFixed: false
        }
        columnAxis: Category3DAxis {
            title: "Category"
            titleVisible: false
        }
        valueAxis: Value3DAxis {
            title: "Expense"
            titleVisible: true
            labelFormat: "%.0f"
            max: dataReader.maxExpense > 0 ? Math.ceil(dataReader.maxExpense * 1.15) : 1000
        }

        Bar3DSeries {
            id: barSeries
            itemLabelFormat: "@rowLabel | @colLabel: @valueLabel"

            ItemModelBarDataProxy {
                itemModel: flatModel
                rowRole: "tsLabel"
                columnRole: "category"
                valueRole: "expense"
            }
        }
    }

    Component.onCompleted: {
        var timespans = dataReader.timespans
        flatModel.clear()
        for (var s = 0; s < timespans.length; s++) {
            var ts = timespans[s]
//            var parts = ts.label.split(" - ")
//            var shortLabel = parts.length === 2
//                ? parts[0].substring(0, 8) + "-" + parts[1].substring(0, 8)
//                : ts.label
            for (var c = 0; c < ts.categories.length; c++) {
                flatModel.append({
                    "tsLabel":  ts.label,
                    "category": ts.categories[c].name,
                    "expense":  ts.categories[c].expense
                })
            }
        }
    }
}
