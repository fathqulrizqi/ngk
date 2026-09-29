$(document).ready(function () {
    if ($("#nsNumberingListTable").length) {
        $("#nsNumberingListTable").tablesorter({
            theme: 'metro-dark',
            widthFixed: true,
            widgets: ['zebra', 'columns', 'filter', 'stickyHeaders'],
        }).tablesorterPager({
            container: $(".pager"),
            output: '{startRow} to {endRow} ({totalRows})',
            size: 50
        });
    } if ($("#nsAppendixListTable").length) {
        $("#nsAppendixListTable").tablesorter({
            theme: 'metro-dark',
            widthFixed: true,
            widgets: ['zebra', 'columns', 'filter', 'stickyHeaders'],
        }).tablesorterPager({
            container: $(".pager"),
            output: '{startRow} to {endRow} ({totalRows})',
            size: 50
        });
    } if ($("#nsDocInitTable").length) {
        $("#nsDocInitTable").tablesorter({
            theme: 'metro-dark',
            widthFixed: true,
            widgets: ['zebra', 'columns', 'filter', 'stickyHeaders'],
        }).tablesorterPager({
            container: $(".pager"),
            output: '{startRow} to {endRow} ({totalRows})',
            size: 50
        });
    }

    //$('.tablesorter-childRow td').hide();
    $('.tablesorter').on('click', '.toggle', function () {
        toggleChild($(this));
        return false;
    });

    function toggleChild(ctr) {
        var currText = ctr.text();
        if ($("." + currText).length) {
            $("." + currText).each(function () {
                $(this).find('td').toggle();
                if ($(this).find(".toggle").length) {
                    var childText = $(this).find(".toggle").text();
                    if ($("." + childText).length) {
                        toggleChild($(this).find(".toggle"));
                    }
                }
            });
        }
    }

    $("#selDoc").change(function () {
        var currVal = $(this).val();
        $("#srModal textarea").val(null);
        $("#hfNSParentID").val(null);
        $(".NSForm select").each(function () {
            $("option:first", this).prop('selected', 'selected')
        });
        $(".NSForm").hide();
        $(".NSForm[data-docid='" + currVal + "']").show("blind");

        $(".docSub").hide();
        if (currVal == 5) {
            $(".docSub").show("blind");
        }
        $("#hfNSMode").val(currVal);
    });

    $(".rbCat").change(function () {
        var currVal = $(this).val();
        $("#srModal textarea").val(null);
        $("#hfNSParentID").val(null);
        $(".NSForm").hide();
        $(".NSForm select").each(function () {
            $("option:first", this).prop('selected', 'selected')
        });

        if (currVal == 1) {
            $("#hfNSMode").val(51);
            $("#TCForm").show("blind");
        } else {
            $("#hfNSMode").val(52);
            $("#TNForm").show("blind");
        }

    });

    $("#srModal").on("show.bs.modal", function () {
        $("#srModal select").each(function () {
            $("option:first", this).prop('selected', 'selected')
        });
        $("#srModal textarea").val(null);
        $(".docSub").hide();
        $(".NSForm").hide();
        $("#hfNSMode").val(0);
        $("#hfNSParentID").val(null);
    });

    $("#btnNSCreate").click(function () {
        var currDocID = $("#selDoc option:selected").val();
        var currNSMode = $("#hfNSMode").val() * 1;
        var currData = {};
        var currUrl = null;
        switch (currNSMode) {
            case 1:
                $("#btnDLSubmit").click();
                //currData = { iInit: $("#selDLInit").val(), iLine: $("#selDLLine").val(), iRemark: $("#txtDLRemark").val(), docid: currDocID };
                //currUrl = "/NGKBusi/PE/NumberingSystem/NewDPPLine";
                break;
            case 2:
                $("#btnDMSubmit").click();
                //currData = { init: $("#selDMInit").val(), line: $("#selDMLine").val(), machine: $("#selDMMachine").val(), process: $("#selDMProcess").val(), docid: currDocID };
                //currUrl = "/NGKBusi/PE/NumberingSystem/NewDPPMachine";
                break;
            case 3:
                $("#btnJDMSubmit").click();
                //currData = { init: $("#selJDMInit").val(), line: $("#selJDMLine").val(), machine: $("#selJDMMachine").val(), drawing: $("#selJDMDrawing").val(), process: $("#selJDMProcess").val(), docid: currDocID };
                //currUrl = "/NGKBusi/PE/NumberingSystem/NewJDM";
                break;
            case 4:
                $("#btnMHSubmit").click();
                //currData = { init: $("#selMHInit").val(), product: $("#selMHProducts").val(), line: $("#selMHLine").val(), part: $("#selMHPartName").val(), docid: currDocID };
                //currUrl = "/NGKBusi/PE/NumberingSystem/NewMatHandling";
                break;
            case 51:
                $("#btnTCSubmit").click();
                //currData = { line: $("#selTCLine").val(), machine: $("#selTCMachine").val(), tool: $("#selTCTools").val(), docid: currDocID };
                //currUrl = "/NGKBusi/PE/NumberingSystem/NewTC";
                break;
            case 52:
                $("#btnTNSubmit").click();
                //currData = { line: $("#selTNLine").val(), machine: $("#selTNMachine").val(), tool: $("#selTNTools").val(), docid: currDocID };
                //currUrl = "/NGKBusi/PE/NumberingSystem/NewTN";
                break;
        }

        //$.ajax({
        //    type: "POST",
        //    url: currUrl,
        //    data: currData,
        //    success: function (data) {
        //        alert(data);
        //    }, error: function () {
        //        alert("Error Occurred, Please try again !");
        //    }
        //});
    });

    $(".addNSChild").click(function () {
        var currDocNumber = $(this).closest("tr").find("td:eq(0)").text();
        $("#srSubModal").modal("show");
        $("#srSubParentNumber").text(currDocNumber.trim());
        $("#hfSubParentNumber").val(currDocNumber.trim());

    });
    $(".editNSItem").click(function () {
        var currDocNumber = $(this).closest("tr").find("td:eq(0)").text();
        var currRemark = $(this).closest("tr").find("td:eq(1)").text();
        $("#srEditModal").modal("show");
        $("#srEditParentNumber").text(currDocNumber.trim());
        $("#hfEditParentNumber").val(currDocNumber.trim());
        $("#txtEditRemark").val(currRemark.trim());
    });

    $(".deleteNSItem").click(function () {
        var currCTR = $(this);
        var currID = currCTR.data("parentid");
        var currTR = currCTR.closest("tr");
        var currDoc = currTR.find("td:eq(0)").text();
        if (confirm("Are you sure want to delete this data?")) {
            currTR.find("td").css("background-color", "orange");
            $.ajax({
                type: "POST",
                url: "/NGKBusi/PE/NumberingSystem/NSDelete",
                data: { parentID: currID, parentDoc: currDoc },
                success: function (data) {
                    if (currCTR.hasClass("deleteChild")) {
                        currTR.prev().find(".deleteNSItem").show();
                    }
                    currTR.remove();
                    $("#nsNumberingListTable").trigger("update");
                }, error: function () {
                    $("#nsNumberingListTable").trigger("update");
                    alert("Error Occurred, Please try again !");
                }
            });
        }
    });

    $("#srSubModal").on("show.bs.modal", function () {
        $("#srSubParentNumber,#txtSubRemark").text(null);
        $("#hfSubParentNumber").val(null);
    });
    $("#srEditModal").on("show.bs.modal", function () {
        $("#srEditParentNumber,#txtEditRemark").text(null);
        $("#hfEditParentNumber").val(null);
    });


    $("#btnAppCreate").click(function (e) {
        e.preventDefault();
        var currAppCode = $("#txtAppCode").val();
        var currCode = $("#txtCode").val();
        $.ajax({
            type: "POST",
            url: "/NGKBusi/PE/NumberingSystem/checkAppendix",
            data: { iAppCode: currAppCode, iCode: currCode },
            success: function (data) {
                if (data > 0) {
                    alert("Appendix Code '" + currAppCode + "' with code '" + currCode.toUpperCase() + "' already exists !");
                    return false;
                } else {
                    $("#NSNewAppForm").submit();
                }
            }, error: function () {
                alert("Error Occurred, Please try again !");
            }
        });
    });

    var currAppEditTR = null;
    $(".editAppItem").click(function () {
        currAppEditTR = $(this).closest("tr");
        var currID = $(this).data("id");
        var currAppCode = currAppEditTR.find("td:eq(0)").text().trim();
        var currCode = currAppEditTR.find("td:eq(1)").text().trim();
        var currRemark = currAppEditTR.find("td:eq(2)").text().trim();
        $("#srEditModal").modal("show");
        $("#txtEditAppCode").val(currAppCode);
        $("#txtEditCode").val(currCode);
        $("#txtEditRemark").val(currRemark);
        $("#hfEditID").val(currID);
    });
    $("#srEditModal").on("show.bs.modal", function () {
        $("#txtEditAppCode,#txtEditCode,#txtEditRemark,#hfEditID").val(null);
    });

    $("#btnAppEdit").click(function () {
        var currID = $("#hfEditID").val();
        var currAppCode = $("#txtEditAppCode").val().toUpperCase();
        var currCode = $("#txtEditCode").val().toUpperCase();
        var currRemark = $("#txtEditRemark").val();
        if ($("#appEditForm").valid()) {
            $.ajax({
                type: "POST",
                url: "/NGKBusi/PE/NumberingSystem/checkAppendix",
                data: { iID: currID, iAppCode: currAppCode, iCode: currCode },
                success: function (data) {
                    if (data > 0) {
                        alert("Appendix Code '" + currAppCode + "' with code '" + currCode + "' already exists !");
                        return false;
                    } else {
                        $.ajax({
                            type: "POST",
                            url: "/NGKBusi/PE/NumberingSystem/editAppendix",
                            data: { iID: currID, iAppCode: currAppCode, iCode: currCode, iRemark: currRemark },
                            success: function (data) {
                                currAppEditTR.find("td:eq(0)").text(currAppCode);
                                currAppEditTR.find("td:eq(1)").text(currCode);
                                currAppEditTR.find("td:eq(2)").text(currRemark);
                                $("#srEditModal").modal("hide");
                            }, error: function () {
                                alert("Error Occurred, Please try again !");
                            }
                        });
                    }
                }, error: function () {
                    alert("Error Occurred, Please try again !");
                }
            });
        }
    });
    $(".deleteAppItem").click(function () {
        var currTR = $(this).closest("tr");
        var currID = $(this).data("id");
        if (confirm("Are you sure want to delete this data ?")) {
            currTR.find("td").css("background-color", "orange");
            $.ajax({
                type: "POST",
                url: "/NGKBusi/PE/NumberingSystem/deleteAppendix",
                data: { iID: currID },
                success: function (data) {
                    currTR.remove();
                    $("#srEditModal").modal("hide");
                    $("#nsAppendixListTable").trigger("update");
                }, error: function () {
                    $("#nsAppendixListTable").trigger("update");
                    alert("Error Occurred, Please try again !");
                }
            });
        }
    });

    $("#btnDocInitAdd").click(function (e) {
        e.preventDefault();
        var currDocID = $("#selDocList option:selected").val();
        var currDocName = $("#selDocList option:selected").text();
        var currCode = $("#txtCode").val();
        $.ajax({
            type: "POST",
            url: "/NGKBusi/PE/NumberingSystem/checkDocInit",
            data: { iDocID: currDocID, iCode: currCode },
            success: function (data) {
                if (data > 0) {
                    alert("Document '" + currDocName + "' with code '" + currCode.toUpperCase() + "' already exists !");
                    return false;
                } else {
                    $("#NSDocInitAddForm").submit();
                }
            }, error: function () {
                alert("Error Occurred, Please try again !");
            }
        });
    });

    $(".deleteDocInitItem").click(function () {
        var currId = $(this).data("id");
        var currTR = $(this).closest("tr");
        if (confirm("Are you sure want to delete this data ?")) {
            currTR.find("td").css("background-color", "orange");
            $.ajax({
                type: "POST",
                url: "/NGKBusi/PE/NumberingSystem/deleteDocInit",
                data: { iID: currId },
                success: function (data) {
                    currTR.remove();
                    $("#nsDocInitTable").trigger("update");
                }, error: function () {
                    $("#nsDocInitTable").trigger("update");
                    alert("Error Occurred, Please try again !");
                }
            });
        }
    });

    var currDocInitTR = null;
    $(".editDocInitItem").click(function () {
        currDocInitTR = $(this).closest("tr");
        var currID = $(this).data("id");
        var currDocName = currDocInitTR.find("td:eq(0)").text();
        var currCode = currDocInitTR.find("td:eq(1)").text().trim();
        var currRemark = currDocInitTR.find("td:eq(2)").text().trim();
        $("#srEditModal").modal("show");
        $("#hfEditDocInitID").val(currID);
        $("#selEditDocList option:contains(" + currDocName + ")").prop("selected", true);
        $("#txtEditCode").val(currCode);
        $("#txtEditRemark").val(currRemark);
    });

    $("#btnEditDocInit").click(function () {
        var currID = $("#hfEditDocInitID").val();
        var currDocID = $("#selEditDocList option:selected").val();
        var currDocName = $("#selEditDocList option:selected").text();
        var currCode = $("#txtEditCode").val().toUpperCase();
        var currRemark = $("#txtEditRemark").val();
        if ($("#NSDocInitEditForm").valid()) {
            $.ajax({
                type: "POST",
                url: "/NGKBusi/PE/NumberingSystem/checkDocInit",
                data: { iID: currID, iDocID: currDocID, iCode: currCode },
                success: function (data) {
                    if (data > 0) {
                        alert("Document '" + currDocName + "' with code '" + currCode + "' already exists !");
                        return false;
                    } else {
                        $.ajax({
                            type: "POST",
                            url: "/NGKBusi/PE/NumberingSystem/editDocInit",
                            data: { iID: currID, iDocID: currDocID, iCode: currCode, iRemark: currRemark },
                            success: function () {
                                currDocInitTR.find("td:eq(0)").text(currDocName);
                                currDocInitTR.find("td:eq(1)").text(currCode);
                                currDocInitTR.find("td:eq(2)").text(currRemark);
                                $("#srEditModal").modal("hide");
                            }, error: function () {
                                alert("Error Occurred, Please try again !");
                            }
                        });
                    }
                }, error: function () {
                    alert("Error Occurred, Please try again !");
                }
            });
        }
    });
});
