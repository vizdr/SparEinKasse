import QtQuick
import QtQuick.Controls.Basic
import QtGraphs

Item {
    id: root
    width: 1024
    height: 800

    ListModel {
        id: flatModel
    }

    Rectangle {
        anchors.fill: parent
        color: "#808090"
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
            topMargin: 6
        }
        spacing: 8

        Text {
            text: "Rotation Y:"
            color: "#e0e0e0"
            font.pixelSize: 13
            anchors.verticalCenter: parent.verticalCenter
        }

        Slider {
            id: rotationSlider
            from: -180; to: 180; value: -23
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

    Row {
        id: rotationRow2
        anchors {
            top: rotationRow.bottom
            horizontalCenter: parent.horizontalCenter
            topMargin: 5
        }
        spacing: 8

        Text {
            text: "Rotation X:"
            color: "#e0e0e0"
            font.pixelSize: 13
            anchors.verticalCenter: parent.verticalCenter
        }

        Slider {
            id: rotationSlider2
            from: -180; to: 180; value: 15
            width: 400
            anchors.verticalCenter: parent.verticalCenter
        }

        Text {
            text: Math.round(rotationSlider2.value) + "°"
            color: "#e0e0e0"
            font.pixelSize: 13
            width: 36
            anchors.verticalCenter: parent.verticalCenter
        }
    }

    Bars3D {
        id: chart
        anchors {
            top: rotationRow2.bottom
            left: parent.left
            right: parent.right
            bottom: parent.bottom
            topMargin: 15
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
        cameraYRotation: rotationSlider2.value
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
            max: (typeof dataReader !== "undefined" && dataReader.maxExpense > 0)
                 ? Math.ceil(dataReader.maxExpense * 1.15) : 1000
        }

        Bar3DSeries {
            itemLabelFormat: "@rowLabel | @colLabel: @valueLabel"

            ItemModelBarDataProxy {
                itemModel: flatModel
                rowRole: "tsLabel"
                columnRole: "category"
                valueRole: "expense"
            }
        }
    }

    Text {
        id: debugOverlay
        z: 200
        anchors { bottom: parent.bottom; left: parent.left; margins: 6 }
        color: "yellow"
        font.pixelSize: 12
        text: ""
    }

    Component.onCompleted: {
        var timespans = []

        if (typeof dataReader !== "undefined") {
            timespans = dataReader.timespans
        } else {
            // QML Preview: load test_sample.json relative to this QML file
            var xhr = new XMLHttpRequest()
            xhr.open("GET", Qt.resolvedUrl("../../test_sample.json"), false)
            xhr.send()
            if (xhr.responseText) {
                try {
                    timespans = JSON.parse(xhr.responseText).timespans || []
                } catch (e) {
                    debugOverlay.text = "Preview: JSON parse error – " + e
                    return
                }
            } else {
                debugOverlay.text = "Preview: could not load test_sample.json"
                return
            }
        }

        flatModel.clear()
        for (var s = 0; s < timespans.length; s++) {
            var ts = timespans[s]
            for (var c = 0; c < ts.categories.length; c++) {
                flatModel.append({
                    "tsLabel":  ts.label,
                    "category": ts.categories[c].name,
                    "expense":  ts.categories[c].expense
                })
            }
        }

        // debugOverlay.text = "timespans=" + timespans.length
        //     + "  flatModel=" + flatModel.count
        //     + ((typeof dataReader !== "undefined" && dataReader.hasError)
        //        ? "  ERR: " + dataReader.errorString : "")
    }
}
