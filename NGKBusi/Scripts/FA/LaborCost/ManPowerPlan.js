$(document).ready(function () {

    var period = parseInt($(".selLCManPowerPlanPeriod").val(), 10);
    $("#btnDownloadMPP").click(function () {
        $("#tblLCManPowerPlan").trigger('outputTable');
    });
    $("#btnDownloadMPPSummary").click(function () {
        $("#tblLCMPPSectionSummary").trigger('outputTable');
    });
    var currTD;

    $(".tdEditRemark").click(function () {
        currTD = $(this);
        var currID = currTD.data('id');
        var currCat = currTD.data('category');
        var currVal = currTD.text();
        $("#modalMPPEditRemark").modal();
        $("#txtMPPEditRemark").val(currVal);
        $("#hfMPPEditRemarkID").val(currID);
        $("#hfMPPEditRemarkCat").val(currCat);
    });

    $("#btnMPPEditRemark").click(function () {
        $("#modalMPPEditRemark").LoadingOverlay("show");
        $.ajax({
            type: "POST",
            url: "/NGKBusi/FA/LaborCost/EditManPowerPlanRemark",
            data: {
                iID: $("#hfMPPEditRemarkID").val(),
                iCat: $("#hfMPPEditRemarkCat").val(),
                iVal: $("#txtMPPEditRemark").val()
            },
            tryCount: 0,
            tryLimit: 3,
            success: function (data) {
                $("#modalMPPEditRemark").LoadingOverlay("hide");
                $('#modalMPPEditRemark').modal('hide');
                currTD.text($("#txtMPPEditRemark").val());
            },
            error: function (textStatus) {
                if (textStatus == "timeout") {
                    this.tryCount++;
                    if (this.tryCount <= this.tryLimit) {
                        $.ajax(this);
                        return;
                    }
                }
                $("#modalMPPEditRemark").LoadingOverlay("hide");
                alert("Error Occurred, Please Try Again.");
            }
        });
    });

    if ($("#tblLCManPowerPlan").length) {
        $("#tblLCManPowerPlan").tablesorter({
            theme: 'metro-dark',
            widthFixed: true,
            widgets: ['zebra', 'columns', 'filter', 'stickyHeaders', 'reflow', 'output'],
            widgetOptions: {
                output_delivery: 'download',
                output_saveFileName: 'Labor_Cost_Man_Power_Plan.csv',
                filter_saveFilters: true,
                stickyHeaders_xScroll: "#divLCManPowerPlan",
                filter_excludeFilter: {
                    4: 'range'
                }
            }
        }).tablesorterPager({
            container: $("#tblLCManPowerPlan .pager"),
            output: '{startRow} to {endRow} ({totalRows})',
            // starting page of the pager (zero based index)
            page: 0,
            // Number of visible rows - default is 10
            size: 10
        });
    }

    if ($("#tblLCMPPStatus").length) {
        $("#tblLCMPPStatus").tablesorter({
            theme: 'metro-dark',
            widthFixed: true,
            widgets: ['zebra', 'columns', 'filter', 'stickyHeaders', 'reflow', 'output'],
            widgetOptions: {
                output_delivery: 'download',
                output_saveFileName: 'Labor_Cost_Man_Power_Plan.csv',
                filter_saveFilters: true,
                stickyHeaders_xScroll: "#tblLCMPPStatusWrapper"
            }
        }).tablesorterPager({
            container: $("#tblLCMPPStatus .pager"),
            output: '{startRow} to {endRow} ({totalRows})',
            // starting page of the pager (zero based index)
            page: 0,
            // Number of visible rows - default is 10
            size: 10
        });
    }

    if ($("#tblLCMPPSectionSummary").length) {
        $("#tblLCMPPSectionSummary").tablesorter({
            theme: 'metro-dark',
            widthFixed: true,
            widgets: ['zebra', 'columns', 'filter', 'stickyHeaders', 'reflow', 'output', 'math'],
            cssChildRow: "tablesorter-childRow",
            widgetOptions: {
                output_delivery: 'download',
                output_saveFileName: 'Labor_Cost_Man_Power_Plan_Summary.csv',
                filter_excludeFilter: {
                    2: 'range'
                },
                math_data: 'math', // data-math attribute
                math_ignore: [0, 1, 2, 3],
                math_none: 'N/A', // no matching math elements found (text added to cell)
                math_complete: function ($cell, wo, result, value, arry) {
                    var txt = '<span class="align-decimal">' +
                        (value === wo.math_none ? '' : '') +
                        result + '</span>';
                    if ($cell.attr('data-math') === 'all-sum') {
                        // when the "all-sum" is processed, add a count to the end
                        //return txt + ' (Sum of ' + arry.length + ' cells)';
                        return txt;
                    }
                    return txt;
                },
                math_completed: function (c) {
                    // c = table.config
                    // called after all math calculations have completed
                    console.log('math calculations complete', c.$table.find('[data-math="all-sum"]:first').text());
                },
                // see "Mask Examples" section
                math_mask: '#.#',
                math_prefix: '', // custom string added before the math_mask value (usually HTML)
                math_suffix: '', // custom string added after the math_mask value
                // event triggered on the table which makes the math widget update all data-math cells (default shown)
                math_event: 'recalculate',
                // math calculation priorities (default shown)... rows are first, then column above/below,
                // then entire column, and lastly "all" which is not included because it should always be last
                math_priority: ['row', 'above', 'below', 'col'],
                // set row filter to limit which table rows are included in the calculation (v2.25.0)
                // e.g. math_rowFilter : ':visible:not(.filtered)' (default behavior when math_rowFilter isn't set)
                // or math_rowFilter : ':visible'; default is an empty string
                math_rowFilter: '',

                scroller_height: 300,
                // scroll tbody to top after sorting
                scroller_upAfterSort: true,
                // pop table header into view while scrolling up the page
                scroller_jumpToHeader: true,
                // In tablesorter v2.19.0 the scroll bar width is auto-detected
                // add a value here to override the auto-detected setting
                scroller_barWidth: null,
                stickyHeaders_xScroll: "#divLCMPPSectionSummary"
            }
        }).tablesorterPager({
            container: $("#tblLCMPPSectionSummary .pager"),
            output: '{startRow} to {endRow} ({totalRows})',
            // starting page of the pager (zero based index)
            page: 0,
            // Number of visible rows - default is 10
            size: 10
        })
            .on('filterStart', function () {
                $('#tblLCMPPSectionSummary').trigger('sortReset');
            })
            .on('tablesorter-initialized filterEnd', function () {
                $(".rowPermanent").each(function (e) {
                    var currRow = $(this);
                    var currSetion = "#rowContract" + currRow.data("section");
                    if ($(currSetion).length > 0) {
                        currRow.insertBefore(currSetion);
                    }
                });
            });
        $(".rowPermanent").each(function (e) {
            var currRow = $(this);
            var currSetion = "#rowContract" + currRow.data("section");
            if ($(currSetion).length > 0) {
                currRow.insertBefore(currSetion);
            }
        });

    }

    $("#btnLCManPowerPlanAdd").click(function () {
        $("#formLCManPowerPlan")[0].reset();
        $(".rowInput").hide();
    });

    $(".cbLCMPPSign").change(function () {
        if ($(".cbLCMPPSign:checked").length > 0) {
            $("#btnLCMPPSign,#btnLCMPPReturn").prop("disabled", false);
        } else {
            $("#btnLCMPPSign,#btnLCMPPReturn").prop("disabled", true);
        }
    });
    $(".cbLCMPPDelete").change(function () {
        if ($(".cbLCMPPDelete:checked").length > 0) {
            $("#btnLCMPPDelete").prop("disabled", false);
        } else {
            $("#btnLCMPPDelete").prop("disabled", true);
        }
    });

    $("#selLCManPowerPlanLevel,#selLCManPowerPlanPeriod").change(function () {
        $("#btnLCManPowerPlanLevel").click();
    });
    $("#selLCPosition").change(function () {
        var currVal = $(this).val();
        if (currVal === "CASUAL") {
            $("#selLCStatus option[value='Contract']").prop("selected", true).change();
        } else {
            $("#selLCStatus option[value='Permanent']").prop("selected", true).change();
        }
    });
    $(".rbLCType").change(function () {
        var currSelected = $(".rbLCType:checked").val();
        $("#formLCManPowerPlan")[0].reset();
        $(".select2").select2({ width: '100%' });
        $(".rbLCType[value='" + currSelected + "']").prop("checked", true);
        $("#txtLCQty").val(1);
        $("#selLCFromMonth,#selLCToMonth").prop("disabled", true);
        switch (currSelected) {
            case "Vacant":
                $("#selLCToMonth,#selLCToYear,#selLCRelated,#selLCSectionAll").prop("required", false).val("").select2();
                $("#txtLCTimePeriod").prop("required", false);
                $(".rowEndDate,.rowRelated,.rowSectionAll").hide();

                $("#selLCSection,#selLCPosition,#selLCFromMonth,#selLCFromYear,#txtLCQty,#selLCStatus").prop("required", true);
                $(".rowSection,.rowPosition,.rowStartDate,.rowQuantity,.rowRemark").show();
                break;
            case "Promosi":
            case "Demosi":
                $("#selLCToMonth,#selLCToYear,#selLCStatus,#selLCSection").prop("required", false).val("").select2();
                $("#txtLCQty,#txtLCTimePeriod").prop("required", false);
                $(".rowEndDate,.rowQuantity,.rowStatus,.rowSection").hide();

                $("#selLCSectionAll,#selLCPosition,#selLCFromMonth,#selLCFromYear,#selLCRelated").prop("required", true);
                $(".rowSectionAll,.rowPosition,.rowRelated,.rowStartDate,.rowRemark").show();
                $("#selLCStatus").val("Permanent");
                break;
            case "Mutasi":
                $("#selLCToMonth,#selLCToYear,#selLCStatus,#selLCSection").prop("required", false).val("").select2();
                $("#txtLCQty,#txtLCTimePeriod,#selLCPosition").prop("required", false);
                $(".rowEndDate,.rowQuantity,.rowPosition,.rowStatus,.rowSection").hide();

                $("#selLCSectionAll,#selLCFromMonth,#selLCFromYear,#selLCRelated").prop("required", true);
                $(".rowSectionAll,.rowRelated,.rowStartDate,.rowRemark").show();
                $("#selLCStatus").val("Permanent");
                break;
            case "Pensiun/Resign":
                $("#selLCSection,#selLCSectionAll,#selLCPosition,#selLCFromMonth,#selLCFromYear,#selLCStatus").prop("required", false).val("").select2();
                $("#txtLCQty,#txtLCTimePeriod").prop("required", false);
                $(".rowQuantity,.rowPosition,.rowSection,.rowSectionAll,.rowStartDate,.rowStatus").hide();


                $("#selLCToMonth,#selLCToYear,#selLCRelated").prop("required", true);
                $(".rowEndDate,.rowRelated,.rowRemark").show();
                $("#selLCStatus").val("Permanent");
                break;
        }
    });
    var fromOpt1 = $("#selLCFromMonth").find("option:not([value=1],[value=2],[value=3],[value=4],[value=5],[value=6],[value=7],[value=8],[value=9])").get();
    var fromOpt2 = $("#selLCFromMonth").find("option").get();
    var fromOpt3 = $("#selLCFromMonth").find("option:not([value=4],[value=5],[value=6],[value=7],[value=8],[value=9],[value=10],[value=11],[value=12])").get();
    var fromOpt4 = $("#selLCFromMonth").find("option:not([value=1],[value=2],[value=3],[value=5],[value=6],[value=7],[value=8],[value=9],[value=11],[value=12])").get();
    $("#selLCFromYear").change(function () {
        getMPPMonth($(this), $("#selLCFromMonth"), fromOpt1, fromOpt2, fromOpt3, fromOpt4);
    });
    $("#selLCToYear").change(function () {
        getMPPMonth($(this), $("#selLCToMonth"), fromOpt1, fromOpt2, fromOpt3, fromOpt4);
    });
    $("#selLCStatus").change(function () {
        var currVal = $(this).val();
        $("#txtLCTimePeriod").val("");
        if (currVal === "Contract") {
            $(".rowTimePeriod").show();
        } else {
            $(".rowTimePeriod").hide();
        }
    });
    $(".btnLCManPowerPlanDelete").click(function () {
        if (confirm("Are you sure want to delete this data ?")) {
            //$("#tblLCManPowerPlan").LoadingOverlay("show");
            //var currTR = $(this).closest("tr");
            //var id = currTR.data("id");
            //currTR.css("background-color", "orange");
            //$.ajax({
            //    type: "POST",
            //    url: "/NGKBusi/FA/LaborCost/ManPowerPlanDelete",
            //    data: {
            //        iID: id,
            //        "iMPPPeriod": period
            //    },
            //    tryCount: 0,
            //    tryLimit: 3,
            //    success: function (data) {
            //        currTR.remove();
            //        $("#tblLCManPowerPlan").LoadingOverlay("hide", true);
            //    },
            //    error: function (textStatus) {
            //        if (textStatus == "timeout") {
            //            this.tryCount++;
            //            if (this.tryCount <= this.tryLimit) {
            //                $.ajax(this);
            //                return;
            //            }
            //        }
            //        currTR.css("background-color", "initial");
            //        alert("Error Occurred, Please Try Again.");
            //        $("#tblLCManPowerPlan").LoadingOverlay("hide", true);
            //    }
            //});
        }
    });
    $("#selLCRelated").change(function () {
        var currPosition = $("option:selected", this).data("position");
        var currSection = $("option:selected", this).data("section");
        var currType = $(".rbLCType:checked").val();
        if (currType === "Pensiun/Resign" || currType === "Promosi" || currType === "Demosi" || currType === "Mutasi") {
            $("#selLCPosition").val(currPosition).select2();
            $("#selLCSection").val(currSection).select2();
            $("#selLCSectionAll").val(currSection).select2();
        }
    });
});
$.tablesorter.equations['contract'] = function ($row, arry) {
    // multiple all array values together
    var product = 1;
    $.each(arry, function (i, v) {
        // oops, we shouldn't have any zero values in the array
        if (v !== 0) {
            product *= v;
        }
    });
    return product;
};

function getMPPMonth(ctrYear, ctrMonth, fromOpt1, fromOpt2, fromOpt3, fromOpt4) {
    var currVal = ctrYear.val();
    var firstVal = ctrYear.find("option:eq(1)").val();
    var lastVal = ctrYear.find("option:last").val();
    var currType = $(".rbLCType:checked").val();
    var getoption;
    if (currVal !== "") {
        if (currType === "Promosi" || currType === "Demosi") {
            getoption = fromOpt4;
        } else {
            switch (currVal) {
                case firstVal:
                    getoption = fromOpt1;
                    break;
                case lastVal:
                    getoption = fromOpt3;
                    break;
                default:
                    getoption = fromOpt2;
                    break;
            }
        }
        ctrMonth.find("option").remove();
        ctrMonth.append(getoption).val('').select2();
        ctrMonth.prop("disabled", false);
    } else {
        ctrMonth.prop("disabled", true);
    }
}