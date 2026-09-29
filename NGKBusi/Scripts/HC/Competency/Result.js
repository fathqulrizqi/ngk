$(document).ready(function () {
    if ($(".tblCRFormList").length > 0) {
        $(".tblCRFormList").tablesorter({            theme: "bootstrap",
            widthFixed: true,

            // widget code contained in the jquery.tablesorter.widgets.js file
            // use the zebra stripe widget if you plan on hiding any rows (filter widget)
            // the uitheme widget is NOT REQUIRED!
            widgets: ["filter", "columns", "stickyHeaders"],

            widgetOptions: {
                //filter_excludeFilter: {
                //    // zero-based column index
                //    7: 'range',
                //    8: 'range',
                //    10: 'range'
                //},
                // class names added to columns when sorted
                columns: ["primary", "secondary", "tertiary"],

                // extra css class name (string or array) added to the filter element (input or select)
                filter_cssFilter: [
                    'form-control',
                    'form-control',
                    'form-control',
                    'form-control',
                    'form-control',
                    'form-control',
                    'form-control',
                    'form-control'
                ], filter_defaultFilter: {
                    // "{query} - a single or double quote signals an exact filter search
                    3: '"{q}',
                    4: '"{q}',
                    5: '"{q}',
                    6: '"{q}'
                }
            }
        }).tablesorterPager({
            cssGoto: '.pagenum',
            container: $(".ts-pager"),
            output: '{startRow} to {endRow} ({totalRows})',
            size: 'all'
        });
    }

    $("#iCRPeriod").change(function () {
        var selectedPeriod = $(this).val();
        var url = new URL(window.location.href);
        url.searchParams.set("iCRPeriod", selectedPeriod);
        window.location.href = url.toString();
    });

    var hotA, hotB, hotC;
    var hotData = [];
    if ($(".divCompetencyResult").length > 0) {
        $(".divCompetencyResult").each(function () {
            generateCompetency($(this));
        });
        const _readOnlyHeader = ["A. Pengetahuan Teknis / Technical Knowledge", "B. Kemampuan Praktik / Practical Skill", "C. Perilaku / Behaviour"]
        const _initMerge = [{ row: 0, col: 0, rowspan: 1, colspan: 12 }];
        function generateCompetency(currDiv) {
            const currID = currDiv.attr("id");
            const currCompetency = currDiv.data("competency");
            const container = document.querySelector('#' + currID);
            $.ajax({
                type: "POST",
                url: "/NGKBusi/HC/Competency/getResultData",
                tryCount: 0,
                tryLimit: 3,
                data: {
                    iPeriodFY: $(".lblNIK").data("periodfy"),
                    iNIK: $(".lblNIK").data("nik"),
                    iName: $(".lblName").data("name"),
                    iDivision: $(".lblDivision").data("division"),
                    iDepartment: $(".lblDepartment").data("department"),
                    iSection: $(".lblSection").data("section"),
                    iCostName: $(".lblCostName").data("costname"),
                    iPosition: $(".lblPosition").data("position"),
                    iTitleName: $(".lblTitleName").data("titlename"),
                    iCompetency: currCompetency,
                },
                success: function (data) {
                    generateHansontable(data, container, currCompetency);
                }, error: function (xhr, textStatus, errorThrown) {
                    if (textStatus === "timeout") {
                        this.tryCount++;
                        if (this.tryCount <= this.tryLimit) {
                            $.ajax(this);
                            return;
                        }
                    }
                    alert("Error Occurred, Please try again !");
                }
            });
        }
        function generateHansontable(currData, container, competency) {
            const hot = new Handsontable(container, {
                nestedHeaders: [
                    ['', '', '', { label: 'HASIL PENILAIAN', colspan: 8 }, ''],
                    ['NO', 'PERSYARATAN KOMPETENSI', 'Std Nilai', '5', '4', '3', '2', '1', '0', 'NILAI', 'HASIL', 'CATATAN']
                ],
                formulas: {
                    engine: HyperFormula,
                },
                data: currData,
                columns: [{ width: '35px', readOnly: true }, { readOnly: true }, { type: 'numeric', width: '35px', className: 'htCenter', readOnly: true, numericFormat: { pattern: '0,00', culture: 'en-US' } }
                    , { type: 'checkbox', width: '35px', className: 'htCenter' }, { type: 'checkbox', width: '35px', className: 'htCenter' }, { type: 'checkbox', width: '35px', className: 'htCenter' }, { type: 'checkbox', width: '35px', className: 'htCenter' }, { type: 'checkbox', width: '35px', className: 'htCenter' }, { type: 'checkbox', width: '35px', className: 'htCenter' }
                    , { type: 'numeric', width: '35px', className: 'htCenter', readOnly: true, numericFormat: { pattern: '0,00', culture: 'en-US' } }
                    , { readOnly: true }, {}],
                colHeaders: true,
                stretchH: 'last',
                height: 'auto',
                width: 'auto',
                mergeCells: true,
                manualColumnResize: true,
                autoWrapRow: true,
                autoWrapCol: true,
                afterLoadData: (sourceData, initialLoad, source) => {
                },
                licenseKey: 'non-commercial-and-evaluation',
            });
            hot.alter('insert_row_below');
            hot.alter('insert_row_below');
            hot.alter('insert_row_below');
            hot.updateSettings({
                cells(row, col) {
                    let upSetting = {}
                    if (_readOnlyHeader.includes(hot.getData()[row][col])) {
                        upSetting.readOnly = true;
                        upSetting.className = "font-weight-bold";
                    };
                    if (row >= this.instance.countRows() - 3) {
                        switch (col) {
                            case 3:
                            case 4:
                            case 5:
                            case 6:
                            case 7:
                            case 8:
                                upSetting.className = "tbFooter htCenter summaryCheckbox";
                                break;
                            case 2:
                            case 9:
                                upSetting.className = "columnSummaryResult tbFooter htCenter";
                                break;
                            default:
                                upSetting.className = "tbFooter htCenter";
                                break;
                        }

                    }
                    return upSetting;
                }, columnSummary: [{
                    sourceColumn: 2,
                    type: 'count',
                    destinationRow: hot.countRows() - 3,
                    destinationColumn: 2,
                    ranges: [[1, hot.countRows() - 3]],
                    forceNumeric: true
                }, {
                    sourceColumn: 2,
                    type: 'sum',
                    destinationRow: hot.countRows() - 2,
                    destinationColumn: 2,
                    ranges: [[1, hot.countRows() - 3]],
                    forceNumeric: true
                }, {
                    sourceColumn: 2,
                    type: 'custom',
                    destinationRow: hot.countRows() - 1,
                    destinationColumn: 2,
                    ranges: [[1, hot.countRows() - 3]],
                    forceNumeric: true,
                    customFunction(endpoint) {
                        var itemCnt = hot.getDataAtCell(endpoint.destinationRow - 2, 2)
                        return itemCnt * 5;
                    }
                }, {
                    sourceColumn: 9,
                    type: 'count',
                    destinationRow: hot.countRows() - 3,
                    destinationColumn: 9,
                    ranges: [[1, hot.countRows() - 3]],
                    forceNumeric: true
                }, {
                    sourceColumn: 9,
                    type: 'sum',
                    destinationRow: hot.countRows() - 2,
                    destinationColumn: 9,
                    ranges: [[1, hot.countRows() - 3]],
                    forceNumeric: true
                }
                    , {
                    sourceColumn: 9,
                    type: 'custom',
                    destinationRow: hot.countRows() - 1,
                    destinationColumn: 9,
                    forceNumeric: true,
                    ranges: [[1, hot.countRows() - 3]],
                    customFunction(endpoint) {

                        var resultScore = "E";
                        if (competency == "A") {
                            var total = hot.getDataAtCell(endpoint.destinationRow, 9);
                            switch (true) {
                                case (total >= 4.51):
                                    resultScore = "A";
                                    break;
                                case (total >= 3.76):
                                    resultScore = "B";
                                    break;
                                case (total >= 2.76):
                                    resultScore = "C";
                                    break;
                                case (total >= 1.26):
                                    resultScore = "D";
                                    break;
                                default:
                                    resultScore = "E";
                                    break;
                            }
                        } else if (competency == "B") {
                            var total = hot.getDataAtCell(endpoint.destinationRow, 9);
                            switch (true) {
                                case (total >= 4.05):
                                    resultScore = "A";
                                    break;
                                case (total >= 3.05):
                                    resultScore = "B";
                                    break;
                                case (total >= 2.05):
                                    resultScore = "C";
                                    break;
                                case (total >= 1.05):
                                    resultScore = "D";
                                    break;
                                default:
                                    resultScore = "E";
                                    break;
                            }
                        } else {
                            var totalPercentage = hot.getDataAtCell(endpoint.destinationRow, 10);
                            switch (true) {
                                case (totalPercentage >= 100):
                                    resultScore = "A";
                                    break;
                                case (totalPercentage >= 80):
                                    resultScore = "B";
                                    break;
                                case (totalPercentage >= 60):
                                    resultScore = "C";
                                    break;
                                case (totalPercentage >= 30):
                                    resultScore = "D";
                                    break;
                                default:
                                    resultScore = "E";
                                    break;
                            }
                        }
                        $(".competencyResult" + competency).text(resultScore);

                        return "=ROUND(SUM(J2:J" + (hot.countRows() - 3) + ")/COUNT(J2:J" + (hot.countRows() - 3) + "),2)";
                    }
                }, {
                    sourceColumn: 10,
                    type: 'custom',
                    destinationRow: hot.countRows() - 1,
                    destinationColumn: 10,
                    ranges: [[1, hot.countRows() - 3]],
                    customFunction(endpoint) {

                        return '=ROUND(COUNTIF(K2:K' + (hot.countRows() - 3) + ',"FIT")/COUNTIF(K2:K' + (hot.countRows() - 3) + ',"<>")*100)';
                    }
                }
                ]
            });



            hot.setDataAtCell(hot.countRows() - 3, 1, "Summary");
            hot.setDataAtCell(hot.countRows() - 2, 1, "Sub-Total");
            hot.setDataAtCell(hot.countRows() - 1, 1, "Total");
            hot.updateSettings({
                mergeCells: _initMerge,
                afterChange: (changes, source) => {
                    if (!changes || changes.length === 0 || source === "score-sync") {
                        return;
                    }

                    var dataRowLimit = hot.countRows() - 3;
                    var updates = [];

                    for (var i = 0; i < changes.length; i++) {
                        var row = changes[i][0];
                        var col = changes[i][1];
                        var oldValue = changes[i][2];
                        var newValue = changes[i][3];

                        if (typeof col !== "number") {
                            col = hot.propToCol(col);
                        }

                        if (oldValue === newValue || row <= 0 || row >= dataRowLimit || col < 3 || col > 8) {
                            continue;
                        }

                        if (newValue === true) {
                            // keep only one checkbox checked in columns 3..8
                            for (var c = 3; c <= 8; c++) {
                                if (c !== col && hot.getDataAtCell(row, c) === true) {
                                    updates.push([row, c, false]);
                                }
                            }

                            // col 3..8 => nilai 5..0
                            updates.push([row, 9, 8 - col]);
                        } else {
                            // if all unchecked, reset nilai to 0
                            var hasChecked = false;
                            for (var c2 = 3; c2 <= 8; c2++) {
                                if (hot.getDataAtCell(row, c2) === true) {
                                    hasChecked = true;
                                    break;
                                }
                            }

                            if (!hasChecked) {
                                updates.push([row, 9, 0]);
                            }
                        }
                    }

                    if (updates.length > 0) {
                        hot.batch(function () {
                            hot.setDataAtCell(updates, "score-sync");
                        });
                    }
                }
            });
            //hot.updateSettings({
            //    mergeCells: _initMerge,
            //    beforeChange: (changes) => {
            //        if (changes != null && changes.length > 0 && changes[0][3] == true && (changes[0][0] > 0 && changes[0][0] < (hot.countRows() - 3))) {
            //            for (var i = 3; i <= 7; i++) {
            //                if (changes[0][1] != i && (changes[0][1] >= 3 && changes[0][1] <= 7)) {
            //                    hot.setDataAtCell(changes[0][0], i, false);
            //                }
            //            }
            //        }
            //    },
            //    afterChange: (changes) => {
            //        if (changes != null && changes.length > 0 && changes[0][3] == true && (changes[0][0] > 0 && changes[0][0] < (hot.countRows() - 3))) {
            //            if (changes[0][1] == 3) {
            //                hot.setDataAtCell(changes[0][0], 8, 5);
            //            } else if (changes[0][1] == 4) {
            //                hot.setDataAtCell(changes[0][0], 8, 4);
            //            } else if (changes[0][1] == 5) {
            //                hot.setDataAtCell(changes[0][0], 8, 3);
            //            } else if (changes[0][1] == 6) {
            //                hot.setDataAtCell(changes[0][0], 8, 2);
            //            } else if (changes[0][1] == 7) {
            //                hot.setDataAtCell(changes[0][0], 8, 1);
            //            }
            //        }
            //    }
            //});
            if (competency == "A") {
                hotA = hot;
            } else if (competency == "B") {
                hotB = hot;
            } else {
                hotC = hot;
            }
            $("#scoreCompetencyResult" + competency).removeClass("d-none");
        }


        $(".btnCRSaveData").click(function () {
            //console.log(hot.validateCells);
            var currCTR = $(this);
            currCTR.LoadingOverlay("show");
            hotData.length = 0;
            hotData.push(hotA.getSourceDataArray().slice(0, -3));
            hotData.push(hotB.getSourceDataArray().slice(0, -3));
            hotData.push(hotC.getSourceDataArray().slice(0, -3));
            hotA.validateCells((valid) => {
                if (valid) {
                    var _currData = hotData;
                    $.ajax({
                        type: "POST",
                        url: "/NGKBusi/HC/Competency/setResultData",
                        tryCount: 0,
                        tryLimit: 3,
                        data: {
                            iPeriodFY: $(".lblNIK").data("periodfy"),
                            iNIK: $(".lblNIK").data("nik"),
                            iName: $(".lblName").data("name"),
                            iDivision: $(".lblDivision").data("division"),
                            iDepartment: $(".lblDepartment").data("department"),
                            iSection: $(".lblSection").data("section"),
                            iCostName: $(".lblCostName").data("costname"),
                            iPosition: $(".lblPosition").data("position"),
                            iTitleName: $(".lblTitleName").data("titlename"),
                            iData: _currData
                        },
                        success: function (data) {
                            swal("Success!", "Data has been saved!", "success");
                            currCTR.LoadingOverlay("hide", true);
                        }, error: function (xhr, textStatus, errorThrown) {
                            if (textStatus === "timeout") {
                                this.tryCount++;
                                if (this.tryCount <= this.tryLimit) {
                                    $.ajax(this);
                                    return;
                                }
                            }
                            console.log(xhr)
                            console.log(textStatus)
                            console.log(errorThrown)
                            currCTR.LoadingOverlay("hide", true);
                            alert("Error Occurred, Please try again !");
                        }
                    });
                } else {
                    swal("Invalid!", "Please check your data format!", "error");
                }
            })
        });

        $(".btnCRRefreshMap").click(function () {
            var currCTR = $(this);
            swal({
                title: "Are you sure?",
                text: "This will refresh the Result data based on the current Competency Map. Any Result items not present in the Map will be deleted, and any new Map items will be added.",
                icon: "warning",
                showCancelButton: true,
                confirmButtonText: "Yes, refresh it!",
                cancelButtonText: "Cancel",
                dangerMode: true,
            }).then((result) => {
                if (!result || !result.isConfirmed) {
                    return;
                }
                currCTR.LoadingOverlay("show");
                $.ajax({
                    type: "POST",
                    url: "/NGKBusi/HC/Competency/RefreshResultMap",
                    tryCount: 0,
                    tryLimit: 3,
                    data: {
                        iPeriodFY: $(".lblNIK").data("periodfy"),
                        iNIK: $(".lblNIK").data("nik"),
                        iName: $(".lblName").data("name"),
                        iDivision: $(".lblDivision").data("division"),
                        iDepartment: $(".lblDepartment").data("department"),
                        iSection: $(".lblSection").data("section"),
                        iCostName: $(".lblCostName").data("costname"),
                        iPosition: $(".lblPosition").data("position"),
                        iTitleName: $(".lblTitleName").data("titlename")
                    },
                    success: function (data) {
                        currCTR.LoadingOverlay("hide", true);
                        if (data.success) {
                            swal({
                                title: "Success!",
                                text: data.message,
                                icon: "success"
                            }).then(() => {
                                location.reload();
                            });
                        } else {
                            swal("Failed!", data.message, "error");
                        }
                    }, error: function (xhr, textStatus, errorThrown) {
                        if (textStatus === "timeout") {
                            this.tryCount++;
                            if (this.tryCount <= this.tryLimit) {
                                $.ajax(this);
                                return;
                            }
                        }
                        console.log(xhr)
                        console.log(textStatus)
                        console.log(errorThrown)
                        currCTR.LoadingOverlay("hide", true);
                        alert("Error Occurred, Please try again !");
                    }
                });
            });
        });
    }
});

