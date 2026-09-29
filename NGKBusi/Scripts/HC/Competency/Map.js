$(document).ready(function () {
    if ($(".tblCMFormList").length > 0) {
        $(".tblCMFormList").tablesorter({
            theme: "bootstrap",
            widthFixed: true,
            widgets: ["filter", "columns", "stickyHeaders"],
            sortList: [[0, 0], [1, 0], [2, 0], [3, 0], [4, 0], [5, 0]],
            widgetOptions: {
                columns: ["primary", "secondary", "tertiary"],
                filter_cssFilter: [
                    'form-control',
                    'form-control',
                    'form-control',
                    'form-control',
                    'form-control',
                    'form-control',
                ], filter_defaultFilter: {
                    0: '"{q}',
                    1: '"{q}',
                    2: '"{q}',
                    3: '"{q}',
                    4: '"{q}'
                }
            }
        }).tablesorterPager({
            cssGoto: '.pagenum',
            container: $(".ts-pager"),
            output: '{startRow} to {endRow} ({totalRows})',
            size: '10'
        });
    }

    if ($("#divCompetencyMap").length > 0) {
        const container = document.querySelector('#divCompetencyMap');
        const _readOnlyHeader = ["A. Pengetahuan Teknis / Technical Knowledge", "B. Kemampuan Praktik / Practical Skill", "C. Perilaku / Behaviour"]
        var _initMerge = [];
        $.ajax({
            type: "POST",
            url: "/NGKBusi/HC/Competency/getMapData",
            tryCount: 0,
            tryLimit: 3,
            data: {
                iDivision: $(".lblDivision").data("division"),
                iDepartment: $(".lblDepartment").data("department"),
                iSection: $(".lblSection").data("section"),
                iCostName: $(".lblCostName").data("costname"),
                iTitleName: $(".lblTitleName").data("titlename"),
                iPosition: $(".lblPosition").data("position"),
            },
            success: function (data) {
                _initMerge = JSON.parse(data.MapMergeCells);
                console.log(data)
                generateHansontable(data);
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
        function generateHansontable(currData) {
            function getSelectionStats(selection) {
                if (!selection || selection.length === 0) {
                    return null;
                }

                var totalRows = 0;
                var minRow = Number.MAX_SAFE_INTEGER;
                var maxRow = -1;

                for (var i = 0; i < selection.length; i++) {
                    var sel = selection[i];
                    var startRow = 0;
                    var endRow = 0;

                    if (Array.isArray(sel)) {
                        startRow = Math.min(sel[0], sel[2]);
                        endRow = Math.max(sel[0], sel[2]);
                    } else {
                        var sRow = sel.start ? sel.start.row : sel.startRow;
                        var eRow = sel.end ? sel.end.row : sel.endRow;
                        startRow = Math.min(sRow, eRow);
                        endRow = Math.max(sRow, eRow);
                    }

                    totalRows += (endRow - startRow + 1);
                    minRow = Math.min(minRow, startRow);
                    maxRow = Math.max(maxRow, endRow);
                }

                return {
                    totalRows: totalRows,
                    minRow: minRow,
                    maxRow: maxRow
                };
            }

            function createInsertRowMenu(type) {
                return {
                    name: type === 'above' ? 'Insert row(s) above' : 'Insert row(s) below',
                    hidden() {
                        var selectedRange = this.getSelectedRangeLast();
                        if (!selectedRange) {
                            return false;
                        }

                        var minSelectedRow = Math.min(selectedRange.from.row, selectedRange.to.row);
                        var maxSelectedRow = Math.max(selectedRange.from.row, selectedRange.to.row);

                        if (type === 'above') {
                            return minSelectedRow <= 0; // first row
                        }

                        return maxSelectedRow >= this.countRows() - 1; // last row
                    },
                    callback: function (key, selection) {
                        var stats = getSelectionStats(selection);
                        if (!stats || stats.totalRows <= 0) {
                            return;
                        }

                        if (type === 'above') {
                            if (stats.minRow <= 0) {
                                return;
                            }

                            this.alter('insert_row_above', stats.minRow, stats.totalRows);
                            return;
                        }

                        var lastRowIndex = this.countRows() - 1;
                        if (stats.maxRow >= lastRowIndex) {
                            return;
                        }

                        this.alter('insert_row_below', stats.maxRow, stats.totalRows);
                    }
                };
            }

            function createPasteIntoNewRowMenu(type) {
                return {
                    name: type === 'at' ? 'Paste into new row (at selected)' : 'Paste into new row (after selected)',
                    hidden() {
                        var selectedRange = this.getSelectedRangeLast();
                        if (!selectedRange) {
                            return true;
                        }

                        var minSelectedRow = Math.min(selectedRange.from.row, selectedRange.to.row);
                        var maxSelectedRow = Math.max(selectedRange.from.row, selectedRange.to.row);

                        if (type === 'at') {
                            return minSelectedRow <= 0;
                        }

                        return maxSelectedRow >= this.countRows() - 1;
                    },
                    callback: function (key, selection) {
                        var self = this;
                        var selectedRange = self.getSelectedRangeLast();

                        if (!selectedRange) {
                            return;
                        }

                        var targetRow = type === 'at' 
                            ? Math.min(selectedRange.from.row, selectedRange.to.row)
                            : Math.max(selectedRange.from.row, selectedRange.to.row) + 1;

                        if (type === 'at' && targetRow <= 0) {
                            return;
                        }

                        var lastRowIndex = self.countRows() - 1;
                        if (type === 'after' && targetRow > lastRowIndex) {
                            return;
                        }

                        navigator.clipboard.readText().then(function(clipboardData) {
                            if (!clipboardData) {
                                return;
                            }

                            var rows = clipboardData.split('\n').filter(function(row) {
                                return row.trim() !== '';
                            });

                            if (rows.length === 0) {
                                return;
                            }

                            var parsedData = rows.map(function(row) {
                                return row.split('\t');
                            });

                            var numRowsToInsert = parsedData.length - 1;

                            if (numRowsToInsert > 0) {
                                self.alter('insert_row_below', targetRow, numRowsToInsert);
                            }

                            for (var i = 0; i < parsedData.length; i++) {
                                var rowData = parsedData[i];
                                var insertRowIndex = targetRow + i;

                                for (var j = 0; j < rowData.length && j < self.countCols(); j++) {
                                    self.setDataAtCell(insertRowIndex, j, rowData[j]);
                                }
                            }
                        }).catch(function(err) {
                            console.error('Failed to read clipboard:', err);
                            alert('Failed to read clipboard. Please ensure clipboard permissions are granted.');
                        });
                    }
                };
            }
            var hot = new Handsontable(container, {
                nestedHeaders: [
                    ['', '', '', '', { label: 'METODE & DURASI PELATIHAN', colspan: 4 }, '', '', ''],
                    ['NO', 'KOMPETENSI', 'NILAI', 'MODUL KOMPETENSI', 'INTERNAL', 'DURASI', 'EXTERNAL', 'DURASI', 'TRAINER', 'METODE EVALUASI', 'CATATAN']
                ],
                data: currData.MapData,
                columns: [{ width: '30px' }, { width: '275px' }, { type: 'numeric', width: '35px', className: 'htCenter', numericFormat: { pattern: '0,00', culture: 'en-US' } }, { width: '275px' },
                {
                    type: 'dropdown',
                    source: [
                        'Self learning',
                        'In class',
                        'Mentoring',
                        'Practical',
                    ], className: 'htCenter', width: '75px'
                }, { type: 'numeric', width: '35px', className: 'htCenter', numericFormat: { pattern: '0,00', culture: 'en-US' } },
                {
                    type: 'dropdown',
                    source: [
                        'Seminar',
                        'Workshop',
                        'Training',
                    ], className: 'htCenter', width: '75px'
                }, { type: 'numeric', width: '35px', className: 'htCenter', numericFormat: { pattern: '0,00', culture: 'en-US' } }, { width: '125px' },
                {
                    width: '125px',
                    type: 'dropdown',
                    source: [
                        'Writen Test',
                        'Practical Test',
                        'Report',
                        'Interview',
                        'Assesment',
                        'Project Assignment',
                    ], className: 'htCenter', width: '75px'
                }, {}
                ],
                contextMenu: {
                    items: {
                        row_above: createInsertRowMenu('above'),
                        row_below: createInsertRowMenu('below'),
                        sep1: '---------',
                        paste_into_new_row_at: createPasteIntoNewRowMenu('at'),
                        sep2: '---------',
                        remove_row: {
                            hidden() {
                                return _readOnlyHeader.includes(this.getValue());
                            }
                        },
                        mergeCells: {
                        },

                    }
                },
                colHeaders: true,
                licenseKey: 'non-commercial-and-evaluation',
            });
            hot.alter('insert_row_below')
            hot.updateSettings({
                cells(row, col) {
                    let upSetting = {}
                    if (_readOnlyHeader.includes(hot.getData()[row][col])) {
                        upSetting.readOnly = true;
                        upSetting.className = "font-weight-bold";
                    };
                    if (row == this.instance.countRows() - 1) {
                        switch (col) {
                            case 2:
                            case 5:
                            case 7:
                                upSetting.className = "columnSummaryResult tbFooter htCenter";
                                break;
                            default:
                                upSetting.className = "tbFooter htRight";
                                break;
                        }

                    }
                    return upSetting;
                }, columnSummary: [{
                    sourceColumn: 2,
                    type: 'sum',
                    destinationRow: hot.countRows() - 1,
                    destinationColumn: 2,
                    forceNumeric: true
                }, {
                    sourceColumn: 5,
                    type: 'sum',
                    destinationRow: hot.countRows() - 1,
                    destinationColumn: 5,
                    forceNumeric: true
                }, {
                    sourceColumn: 7,
                    type: 'sum',
                    destinationRow: hot.countRows() - 1,
                    destinationColumn: 7,
                    forceNumeric: true
                }]
            });
            if (_initMerge != null && _initMerge.length == 0) {
                _initMerge.push({ row: hot.countRows() - 1, col: 3, rowspan: 1, colspan: 2 });
            }
            if (_initMerge != null && _initMerge.length > 0) {
                hot.updateSettings({
                    mergeCells: _initMerge
                });
            }
            hot.setDataAtCell(hot.countRows() - 1, 1, "TOTAL NILAI");
            hot.setDataAtCell(hot.countRows() - 1, 3, "TOTAL DURASI (JAM)");

            $(".btnCMSaveData").click(function () {
                //console.log(hot.validateCells);
                var _mergeCellSetting = JSON.stringify(hot.getPlugin('mergeCells').mergedCellsCollection.mergedCells);
                hot.validateCells((valid) => {
                    if (valid) {
                        var _currData = hot.getSourceDataArray();
                        $.ajax({
                            type: "POST",
                            url: "/NGKBusi/HC/Competency/setMapData",
                            tryCount: 0,
                            tryLimit: 3,
                            data: {
                                iDivision: $(".lblDivision").data("division"),
                                iDepartment: $(".lblDepartment").data("department"),
                                iSection: $(".lblSection").data("section"),
                                iCostName: $(".lblCostName").data("costname"),
                                iTitleName: $(".lblTitleName").data("titlename"),
                                iPosition: $(".lblPosition").data("position"),
                                iData: _currData.slice(0, -1),
                                iMergeCells: _mergeCellSetting
                            },
                            success: function (data) {
                                //console.log(_currData);
                                //console.log(data);
                                swal("Success!", "Data has been saved!", "success");
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
                                alert("Error Occurred, Please try again !");
                            }
                        });
                    } else {
                        swal("Invalid!", "Please check your data format!", "error");
                    }
                })
            });
            //hot.addHook('afterMergeCells', (cellRange, mergeParent) => {
            //    //const _cellRangeFromRow = cellRange.from.row;
            //    //const _cellRangeFromCol = cellRange.from.col;
            //    //const _cellRangeToRow = cellRange.to.row;
            //    //const _cellRangeToCol = cellRange.to.col;

            //    //console.log(cellRange);
            //    //console.log(_cellRangeFromRow);
            //    //console.log(_cellRangeFromCol);
            //});

        }
    }
});

