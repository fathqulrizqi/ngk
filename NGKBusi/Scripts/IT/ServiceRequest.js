$(document).ready(function () {
    if ($("#srListTable").length) {
        $("#srListTable").tablesorter({
            theme: 'metro-dark',
            widthFixed: true,
            widgets: ['zebra', 'columns', 'filter', 'stickyHeaders', 'reflow'],
            widgetOptions: {
                filter_saveFilters: true
            }
        }).tablesorterPager({
            container: $(".pager"),
            output: '{startRow} to {endRow} ({totalRows})'
        });
    }

    if ($("#srReportTable").length) {
        $("#srReportTable,#srCreateReportTable").tablesorter({
            theme: 'metro-dark',
            widthFixed: true,
            widgets: ['zebra', 'columns', 'filter', 'stickyHeaders'],
            widgetOptions: {
                filter_saveFilters: true
            }
        }).tablesorterPager({
            container: $(".pager"),
            output: '{startRow} to {endRow} ({totalRows})'
        });
    }

    $("#selUserName").change(function () {
        var loadingBar = '<img id="imgLoader" src="../../Content/Images/loading-circle-oval.gif" width="30" height="30" />';
        $("#hfSectionID").parent().append(loadingBar);
        $("#issuedSection").hide();
        $.ajax({
            type: "POST",
            url: '/NGKBusi/IT/ServiceRequest/getSection',
            data: {
                userID: $(this).val()
            },
            success: function (data) {
                var sectionID = data.DeptID;
                var section = data.DeptName;
                $("#issuedSection").text(section);
                $("#hfSectionID").val(sectionID);
                $("#issuedSection").show();
                $("#imgLoader").remove();
            },
            error: function () {
                $("#imgLoader").remove();
                $("#issuedSection").show();
                alert("Error occurred, please try again.");
            }
        });
    });


    $("#srListTable .rateColumn").each(function () {
        var currTR = this.closest("tr");
        var currRate = $(currTR).find("#hfData").data("rate");
        setRate(currRate, currTR);
    });
    $(".srDoneRate").click(function () {
        if ($(this).attr("data-disabled") == "false") {
            var minRate = 1;
            var maxRate = 5;
            var currRate = $(this).data("rate");
            $(".srDoneRate").attr("src", "../../Content/Images/grey-star.png");
            for (var i = 1; i <= currRate; i++) {
                $(".srDoneRate[data-rate=" + i + "]").attr("src", "../../Content/Images/gold-star.png");
            }

            $("#hfDoneRate").val(currRate);
            $("#srRateWrap").css("border", "");
        }
    });

    $(".srRate").click(function () {
        var currTR = $(this).closest("tr");
        curDoneTR = currTR;
        var currID = $(currTR).find("#hfData").data("id");
        curDoneID = currID;
        var currRate = $(currTR).find("#hfData").data("rate");
        var currReqUserID = $(currTR).find("#hfData").data("requserid");
        var currReqUserName = $(currTR).find("#hfData").data("requsername");
        var currLoggedUserID = $(currTR).find("#hfData").data("loggeduserid");
        var currStatusID = $(currTR).find("#hfData").data("statusid");
        var currComment = $(currTR).find("#hfData").data("comment");
        var currRateGroup = $(this).closest("td").find(".rateStar");
        var ctrDisabled = $(this).data("disabled");
        var ctrRate = $(this).data("rate");
        var ctrRateDate = $(currTR).find("#hfData").data("ratedate");
        var currClientDate = $.datepicker.formatDate('yymmdd', new Date());

        if (currStatusID != 5) {
            alert("We're sorry but this request has not been done yet.");
            return false;
        }

        $("#txtComment").attr("disabled", false);
        $(".srDoneRate").attr("data-disabled", false);
        if (currReqUserID != currLoggedUserID) {
            $(".srDoneRate").attr("data-disabled", true);
            $("#txtComment").prop("disabled", true);
            //alert("We're sorry but only user '" + currReqUserName + "' can rate this request.");
            //return false;
        }

        $("#srDoneModal").modal("show");
        $("#srDoneModal #txtComment").val(currComment);
        $("#srDoneModal #hfDoneRate").val(currRate)
        for (var i = 1; i <= currRate; i++) {
            $("#srDoneModal .srDoneRate[data-rate=" + i + "]").attr("src", "../../Content/Images/gold-star.png");
        }

        if ((currClientDate - ctrRateDate) > 0) {
            $(".srDoneRate").attr("data-disabled", true);
            $("#txtComment").prop("disabled", true);
            $("#srDoneModal .modal-footer").hide();
            $("#srDoneModal #txtComment").attr("readonly", true);
        }


        //if (!ctrDisabled) {
        //if (confirm("Are you sure want to rate " + ctrRate + " this request ?")) {
        //    var loadingBar = '<img id="imgLoader" src="../../Content/Images/loading-bar-blue.gif" width="125" height="25" />';
        //    currRateGroup.hide();
        //    $(this).closest("td").append(loadingBar);
        //    $.ajax({
        //        type: "POST",
        //        url: '/NGKBusi/IT/ServiceRequest/setRate',
        //        data: {
        //            id: currID,
        //            rate: ctrRate
        //        },
        //        success: function (data) {
        //            setRate(ctrRate, currTR);
        //            $(currTR).find("#hfData").data("rate", ctrRate);
        //            $(currTR).find("#hfData").data("ratedate", data);
        //            $(".srRate").text(ctrRate);

        //            currRateGroup.show();
        //            $("#imgLoader").remove();
        //            alert("Thank you for giving us rating for this request. :)");
        //        },
        //        error: function () {
        //            $("#imgLoader").remove();
        //            currRateGroup.show();
        //            alert("Error occurred, please try again.");
        //        }
        //    });
        //}
        //} else {
        //    alert("We're sorry but ticket has been closed.")
        //}
    });

    var curEditID = 0;
    var curEditTR = null;
    $(".srEdit").click(function () {
        var currTR = $(this).closest("tr");
        curEditTR = currTR;
        var currID = $(currTR).find("#hfData").data("id");
        var currReqUserID = $(currTR).find("#hfData").data("requserid");
        var currReqUserName = $(currTR).find("#hfData").data("requsername");
        var currLoggedUserID = $(currTR).find("#hfData").data("loggeduserid");
        var currLoggedSectionID = $(currTR).find("#hfData").data("loggedsectionid");
        var currSection = $(currTR).find("#hfData").data("section");
        var currSectionID = $(currTR).find("#hfData").data("sectionid");
        var currReportID = $(currTR).find("#hfData").data("reportid");
        var currCategoryID = $(currTR).find("#hfData").data("categoryid");
        var currDetail = $(currTR).find("td:eq(3)").text();
        var currITStaffArray = $(currTR).find("td:eq(5)").data("actionbyid").split("/");
        var currAction = $(currTR).find("td:eq(4)").text();
        var currJobstart = $(currTR).find(".srJobstart").text().split(" ");
        var currJobend = $(currTR).find(".srJobend").text().split(" ");
        $('#srModal').modal();
        $("#srModal .modal-title").text("Edit Service Request");
        $("#selUserName option[value='" + currReqUserID + "']").prop("selected", true);
        $("#spanUserName").text(currReqUserName);
        $("#issuedSection").text(currSection);
        $("#hfSectionID").val(currSectionID);
        $("#txtDetail").val(currDetail);
        $("#txtAction").val(currAction);
        $("#txtJobStartDate").val(currJobstart[0]);
        $("#txtJobStartTime").val(currJobstart[1]);
        $("#txtJobEndDate").val(currJobend[0]);
        $("#txtJobEndTime").val(currJobend[1]);
        $(".rbVia[value='" + currReportID + "']").prop("checked", true);
        $(".rbCat[value='" + currCategoryID + "']").prop("checked", true);
        $.each(currITStaffArray, function (i, e) {
            $("#selITStaff option[value='" + e + "']").prop("selected", true);
        });
        if (currReqUserID != currLoggedUserID) {
            $("#selUserName,#txtDetail,.rbVia,#txtJobStartDate,#txtJobEndDate,#txtJobStartTime,#txtJobEndTime").prop("disabled", true);
        }
        $("#selUserName,#selITStaff").select2();
        $("#btnSRSubmit").hide();
        $("#btnSREdit").show();
        curEditID = currID;
    });
    $("#srModal").on('show.bs.modal', function () {
        defaultSRForm();
    });

    $("#btnSREdit").click(function () {
        var usrID = $("#selUserName").val();
        var usrName = $("#selUserName option:selected").text();
        var dtl = $("#txtDetail").val();
        var via = $(".rbVia:checked").val();
        var cat = $(".rbCat:checked").val();
        var catText = $(".rbCat:checked").closest("label").text();
        var member = $("#selITStaff").val();
        var memberName = $("#selITStaff option:selected").text();
        var act = $("#txtAction").val();
        var sectionID = $("#hfSectionID").val();
        var section = $("#issuedSection").text();
        var jobStartDate = $("#txtJobStartDate").val();
        var jobStartTime = $("#txtJobStartTime").val();
        var jobEndDate = $("#txtJobEndDate").val();
        var jobEndTime = $("#txtJobEndTime").val();
        var cat = $(".rbCat:checked").val();
        if (cat == null || cat == undefined) {
            alert("Please fill 'Category' field !");
            return false;
        }
        if (member == null || member == undefined) {
            alert("Please fill 'IT Member' field !");
            return false;
        }
        if (confirm("Are you sure want to edit this data ?")) {
            $.ajax({
                type: "POST",
                url: '/NGKBusi/IT/ServiceRequest/editList',
                data: {
                    id: curEditID,
                    iUserID: usrID,
                    iDetail: dtl,
                    iCategory: cat,
                    iVia: via,
                    iITStaff: member,
                    iSectionID: sectionID,
                    iAction: act,
                    iJobStartDate: jobStartDate,
                    iJobStartTime: jobStartTime,
                    iJobEndDate: jobEndDate,
                    iJobEndTime: jobEndTime
                },
                success: function (data) {
                    curEditTR.find("td:eq(3)").text(dtl);
                    curEditTR.find("td:eq(2)").text(catText);
                    curEditTR.find("td:eq(5)").text(("" + memberName).replace(/,/g, "/"));
                    curEditTR.find("td:eq(5)").attr("data-actionbyid", member);
                    curEditTR.find("#hfData").data("requserid", usrID);
                    curEditTR.find("#hfData").data("requsername", usrName);
                    curEditTR.find("#hfData").data("section", section);
                    curEditTR.find("#hfData").data("sectionid", sectionID);
                    curEditTR.find("#hfData").data("categoryid", cat);
                    curEditTR.find("td:eq(1)").text(usrName);
                    curEditTR.find("td:eq(4)").text(act);
                    curEditTR.find(".srJobstart").text(jobStartDate + " " + jobStartTime);
                    curEditTR.find(".srJobend").text(jobEndDate + " " + jobEndTime);

                    $("#srModal").modal("hide");
                },
                error: function () {
                    alert("Error occurred, please try again.");
                }
            });
        }
    });

    $(".srCancel").click(function () {
        var currTR = $(this).closest("tr");
        var currID = $(currTR).find("#hfData").data("id");
        if (confirm("Are you sure want to cancel this request ?")) {
            $.ajax({
                type: "POST",
                url: '/NGKBusi/IT/ServiceRequest/cancelRequest',
                data: {
                    id: currID
                },
                success: function (data) {
                    currTR.find("#hfData").data("statusid", 1);
                    currTR.find("td:eq(9)").text("Cancel");
                    currTR.find(".srCancel").hide();
                    currTR.find(".srContinue").show();
                },
                error: function () {
                    alert("Error occurred, please try again.");
                }
            });
        }
    });

    $(".srContinue").click(function () {
        var currTR = $(this).closest("tr");
        var currID = $(currTR).find("#hfData").data("id");
        if (confirm("Are you sure want to resend this request ?")) {
            $.ajax({
                type: "POST",
                url: '/NGKBusi/IT/ServiceRequest/continueRequest',
                data: {
                    id: currID
                },
                success: function (data) {
                    currTR.find("#hfData").data("statusid", 2);
                    currTR.find("td:eq(9)").text("Issued");
                    currTR.find("td:eq(0)").text(data);
                    currTR.find(".srCancel").show();
                    currTR.find(".srContinue").hide();
                },
                error: function () {
                    alert("Error occurred, please try again.");
                }
            });
        }
    });

    $(".srStart").click(function () {
        var currTR = $(this).closest("tr");
        var currID = $(currTR).find("#hfData").data("id");
        var currLoggedUser = $(currTR).find("#hfData").data("loggeduserid");
        if (confirm("Are you sure want to start this request ?")) {
            $.ajax({
                type: "POST",
                url: '/NGKBusi/IT/ServiceRequest/StartRequest',
                data: {
                    id: currID
                },
                success: function (data) {
                    currTR.find("#hfData").data("statusid", 3);
                    currTR.find("td:eq(9)").text("On Progress");
                    currTR.find("td:eq(5)").text(currLoggedUser);
                    currTR.find(".srJobstart").text(data);
                    currTR.find(".srStart").hide();
                    currTR.find(".srHold").show();
                    currTR.find(".srEdit").show();

                },
                error: function () {
                    alert("Error occurred, please try again.");
                }
            });
        }
    });

    $(".srHold").click(function () {
        var currTR = $(this).closest("tr");
        var currID = $(currTR).find("#hfData").data("id");
        if (confirm("Are you sure want to hold this request ?")) {
            $.ajax({
                type: "POST",
                url: '/NGKBusi/IT/ServiceRequest/HoldRequest',
                data: {
                    id: currID
                },
                success: function (data) {
                    currTR.find("#hfData").data("statusid", 4);
                    currTR.find("td:eq(9)").text("Hold");
                    currTR.find(".srStart").show();
                    currTR.find(".srHold").hide();
                },
                error: function () {
                    alert("Error occurred, please try again.");
                }
            });
        }
    });

    $("#srDoneModal").on('show.bs.modal', function () {
        $("#srDoneModal #hfDoneRate").val(0);
        $("#srDoneModal textarea").val(null);
        $("#srDoneModal #txtComment").attr("readonly", false);
        $("#srDoneModal .srDoneRate").each(function () {
            $(this).data("disabled", false).attr("src", "../../Content/Images/grey-star.png");
        });
        $("#srDoneModal #srRateWrap").css("border", "");
        $("#srDoneModal .modal-footer").show();
        if (doneModalTxtBorder != null) {
            $("#srDoneModal textarea").css("border", doneModalTxtBorder);
        }
    });

    var curDoneID = 0;
    var curDoneCTR = null;
    var curDoneTR = null;
    $(".srDone").click(function () {
        var currTR = $(this).closest("tr");
        var currID = $(currTR).find("#hfData").data("id");
        curDoneID = currID;
        curDoneCTR = $(this);
        curDoneTR = currTR;
    });
    var doneModalTxtBorder = null;
    $("#srDoneModal textarea").keyup(function () {
        if (doneModalTxtBorder != null) {
            $("#srDoneModal textarea").css("border", doneModalTxtBorder);
        }
    });
    $("#btnDone").click(function () {
        var currCtr = $(this);
        var currComment = $("#srDoneModal textarea").val();
        var currRate = $("#srDoneModal #hfDoneRate").val();
        doneModalTxtBorder = $("#srDoneModal textarea").css("border");
        $("#srDoneModal textarea").css("border", doneModalTxtBorder);
        if (currComment == "") {
            $("#srDoneModal textarea").css("border", "2px solid red");
        }
        if (currRate == 0) {
            $("#srRateWrap").css("border", "2px solid red");
        }
        if (currRate == 0 || currComment == "") {
            return false;
        }
        curDoneTR.find(".srRate").attr("src", "../../Content/Images/grey-star.png");
        for (var i = 1; i <= currRate; i++) {
            curDoneTR.find(".srRate[data-rate=" + i + "]").attr("src", "../../Content/Images/gold-star.png");
        }
        $.ajax({
            type: "POST",
            url: '/NGKBusi/IT/ServiceRequest/DoneRequest',
            data: {
                id: curDoneID,
                comment: currComment,
                rate: currRate
            },
            success: function (data) {
                curDoneTR.find("#hfData").data("statusid", 5);
                curDoneTR.find("#hfData").data("rate", currRate);
                curDoneTR.find("#hfData").data("ratedate", moment(new Date(data)).format("YYYYMMDD"));
                curDoneTR.find("#hfData").data("comment", currComment);
                curDoneTR.find("td:eq(9)").text("Done");
                curDoneTR.find(".srJobend").text(data);
                $("#srDoneModal").modal("hide");
                if (curDoneCTR != null) {
                    curDoneCTR.hide();
                }
                curDoneTR.find(".srHold").hide();
            },
            error: function () {
                alert("Error occurred, please try again.");
            }
        });
    });
    $("#txtJobStartDate,#txtJobEndDate").datepicker({
        dateFormat: "dd-M-yy",
        onSelect: function (dt) {
            var currID = $(this).attr("id");
            if (currID == "txtJobStartDate") {
                $("#txtJobEndDate").datepicker("option", { minDate: new Date(dt) });
            } else {
                $("#txtJobStartDate").datepicker("option", { maxDate: new Date(dt) });
            }
        }
    });
    $("#txtJobStartTime,#txtJobEndTime").clockpicker({
        placement: 'top',
        autoclose: true,
        beforeShow: function () {
            //alert($(".clockpicker-hours .clockpicker-tick", this).filter(function () {
            //    return $(this).text() === "16";
            //}).html());
        }
    });

    $(".cbCreateList").click(function () {
        changeRowColor($(this));
    });
    $("#cbSelectAll").click(function () {
        var isChecked = false;
        if ($(this).is(":checked")) {
            var isChecked = true;
        }
        $(".cbCreateList").each(function () {
            if (!$(this).closest("tr").hasClass("filtered")) {
                $(this).prop("checked", isChecked);
            }
            changeRowColor($(this));
        });
    });
    $("#btnCreate").click(function () {
        var selID = [];
        $(".cbCreateList:checked").each(function () {
            selID.push($(this).data("id"));
        });
        if (selID.length > 0) {
            if (confirm("Are you sure want to create this report ?")) {
                $.ajax({
                    type: "POST",
                    url: '/NGKBusi/IT/ServiceRequest/CreateReport',
                    data: {
                        id: selID
                    },
                    success: function (data) {
                        location.reload();
                    },
                    error: function () {
                        alert("Error occurred, please try again.");
                    }
                });
            }
        }
    });

    var currDisplayedReportID = null;
    $(".srReportDisplay").click(function () {
        var currID = $(this).data("id");
        currDisplayedReportID = currID;
        $.ajax({
            type: "POST",
            url: '/NGKBusi/IT/ServiceRequest/getReport',
            data: {
                id: currID
            },
            success: function (data) {
                var dataTR = "";
                var createdByID = null;
                var createdByName = null;
                var createdDate = null;
                var checkedByID = null;
                var checkedByName = null;
                var checkedDate = null;
                var approvedByID = null;
                var approvedByName = null;
                var approvedDate = null;
                var rowCount = 0;
                $.each(data, function (e, i) {
                    rowCount++;
                    var js = moment(new Date(parseInt(i.Data.JobStart.substr(6))));
                    var je = moment(new Date(parseInt(i.Data.JobEnd.substr(6))));
                    var diff = js.to(je, true);
                    var total = diff;
                    dataTR += "<tr>"
                        + "<td nowrap style='padding:5px;text-align:center;'>" + moment(new Date(parseInt(i.Data.IssuedDate.substr(6)))).format('DD-MMM-YYYY') + "</td>"
                        + "<td style='padding:5px;'>" + i.Data.Detail + "</td>"
                        + "<td style='padding:5px;text-align:center;'>" + i.ReportName + "</td>"
                        + "<td style='padding:5px;'>" + i.Data.Action + "</td>"
                        + "<td nowrap style='padding:5px;text-align:center;'>" + js.format('DD-MMM-YYYY HH:mm') + "</td>"
                        + "<td nowrap style='padding:5px;text-align:center;'>" + je.format('DD-MMM-YYYY HH:mm') + "</td>"
                        + "<td style='padding:5px;text-align:center;'>" + total + "</td>"
                        + "<td style='text-align:center;vertical-align:middle;height:12px;'>" + "<img src='" + "../../Content/Sign/" + i.Data.ActionBy + ".png" + "' style='height:100%;width:auto;' />" + "</td>"
                        + "<td nowrap style='padding:5px;text-align:center;'>" + i.DataActionBy + "</td>"
                        + "<td style='text-align:center;vertical-align:middle;height:12px;'>" + "<img src='" + "../../Content/Sign/" + i.Data.ForemanBy + ".png" + "' style='height:100%;width:auto;' />" + "</td>"
                        + "<td nowrap style='padding:5px;text-align:center;'>" + i.DataForemanBy + "</td>"
                        + "</tr>";

                    createdByID = i.HeaderCreatedByID;
                    createdByName = (i.HeaderCreatedDate == null ? null : i.HeaderCreatedByName);
                    createdDate = (i.HeaderCreatedDate == null ? "../../...." : moment(new Date(parseInt(i.HeaderCreatedDate.substr(6)))).format('DD/MM/YYYY'));
                    checkedByID = i.HeaderCheckedByID;
                    checkedByName = (i.HeaderCheckedDate == null ? null : i.HeaderCheckedByName);
                    checkedDate = (i.HeaderCheckedDate == null ? "../../...." : moment(new Date(parseInt(i.HeaderCheckedDate.substr(6)))).format('DD/MM/YYYY'));
                    approvedByID = i.HeaderApprovedByID;
                    approvedByName = (i.HeaderApprovedDate == null ? null : i.HeaderApprovedByName);
                    approvedDate = (i.HeaderApprovedDate == null ? "../../...." : moment(new Date(parseInt(i.HeaderApprovedDate.substr(6)))).format('D/MM/YYYY'));
                });
                for (i = rowCount; i <= 20; i++) {
                    dataTR += "<tr>"
                        + "<td nowrap style='padding:5px;text-align:center;height:12px;'></td>"
                        + "<td style='padding:5px;'></td>"
                        + "<td style='padding:5px;text-align:center;'></td>"
                        + "<td style='padding:5px;'></td>"
                        + "<td nowrap style='padding:5px;text-align:center;'></td>"
                        + "<td nowrap' style='padding:5px;text-align:center;'></td>"
                        + "<td style='padding:5px;text-align:center;'></td>"
                        + "<td style='padding:5px;text-align:center;'></td>"
                        + "<td style='padding:5px;text-align:center;'></td>"
                        + "<td style='padding:5px;text-align:center;'></td>"
                        + "<td style='padding:5px;text-align:center;'></td>"
                        + "</tr>";
                }
                $("#lblCreatedBy").text(createdByName);
                $("#lblCreatedBy").data("id", createdByID);
                $("#lblCreatedDate").text(createdDate);
                $("#lblCheckedBy").text(checkedByName);
                $("#lblCreatedBy").data("id", checkedByID);
                $("#lblCheckedDate").text(checkedDate);
                $("#lblApprovedBy").text(approvedByName);
                $("#lblCreatedBy").data("id", approvedByID);
                $("#lblApprovedDate").text(approvedDate);
                $("#createdSign,#checkedSign,#approvedSign").css("background-image", "")
                    .css("background-size", "").css("background-repeat", "").css("background-position", "")
                    .css("margin-top", "");
                $("#createdSign").css("background-image", "url('../../Content/Sign/" + createdByID + ".png')")
                    .css("background-size", "contain").css("background-repeat", "no-repeat").css("background-position", "center")
                    .css("margin", "5px");
                if (checkedDate != "../../....") {
                    $("#checkedSign").css("background-image", "url('../../Content/Sign/" + checkedByID + ".png')")
                        .css("background-size", "contain").css("background-repeat", "no-repeat").css("background-position", "center")
                        .css("margin", "5px");
                }
                if (approvedDate != "../../....") {
                    $("#approvedSign").css("background-image", "url('../../Content/Sign/" + approvedByID + ".png')")
                        .css("background-size", "contain").css("background-repeat", "no-repeat").css("background-position", "center")
                        .css("margin", "5px");
                }
                $("#srReportFormDialog").modal("show");
                $("#tblSRItem tbody").html(dataTR);
                $("#imgCheck,#imgApprove").hide();
                if ($("#mainLoggedUser").data("id") == checkedByID && checkedDate == "../../....") {
                    $("#imgCheck").show();
                } else if ($("#mainLoggedUser").data("id") == approvedByID && checkedDate != "../../...." && approvedDate == "../../....") {
                    $("#imgApprove").show();
                }
            },
            error: function () {
                alert("Error occurred, please try again.");
            }
        });
    });
    $("#imgCheck").click(function () {
        var currCtr = $(this);
        if (confirm("Are you sure want to sign this document ?")) {
            $.ajax({
                type: "POST",
                url: '/NGKBusi/IT/ServiceRequest/reportChecked',
                data: {
                    id: currDisplayedReportID
                },
                success: function (data) {
                    $("#checkedSign").css("background-image", "url('../../Content/Sign/" + $("#mainLoggedUser").data("id") + ".png')")
                        .css("background-size", "contain").css("background-repeat", "no-repeat").css("background-position", "center")
                        .css("margin", "5px");
                    var signDate = moment(new Date(parseInt(data.checkedDate.substr(6))));
                    $("#lblCheckedDate").text(signDate.format('DD/MM/YYYY'));
                    $("#lblCheckedBy").text(data.checkedName);
                    $("#lblCheckedBy").data("id", data.checkedID);
                    currCtr.hide();
                    $("#imgApprove").show();
                    $(".srReportDisplay[data-id=" + currDisplayedReportID + "]").closest("tr").find("td:eq(4) span").text("Checked");
                    $('#srReportTable').trigger('update');
                },
                error: function () {
                    alert("Error occurred, please try again.");
                }
            });
        }
    });
    $("#imgApprove").click(function () {
        var currCtr = $(this);
        if (confirm("Are you sure want to sign this document ?")) {
            $.ajax({
                type: "POST",
                url: '/NGKBusi/IT/ServiceRequest/reportApproved',
                data: {
                    id: currDisplayedReportID
                },
                success: function (data) {
                    $("#approvedSign").css("background-image", "url('../../Content/Sign/" + $("#mainLoggedUser").data("id") + ".png')")
                        .css("background-size", "contain").css("background-repeat", "no-repeat").css("background-position", "center")
                        .css("margin", "5px");
                    var signDate = moment(new Date(parseInt(data.approvedDate.substr(6))));
                    $("#lblApprovedDate").text(signDate.format('DD/MM/YYYY'));
                    $("#lblApprovedBy").text(data.approvedName);
                    $("#lblApprovedBy").data("id", data.approvedID);
                    currCtr.hide();
                    $(".srReportDisplay[data-id=" + currDisplayedReportID + "]").closest("tr").find("td:eq(4) span").text("Approved");
                    $('#srReportTable').trigger('update');
                },
                error: function () {
                    alert("Error occurred, please try again.");
                }
            });
        }
    });

    $("#btnReportPrint").click(function () {
        printElement($(".srReportForm").parent());
    });
});

function changeRowColor(ctr) {
    var currTR = ctr.closest("tr");
    var currIdx = currTR.index() % 2;
    var currCreateBGColor = "#fff";
    var ctrCnt = $("#srCreateReportTable").find("tr[class!='filtered']").find(".cbCreateList").length;
    var ctrChecked = $("#srCreateReportTable").find("tr[class!='filtered']").find(".cbCreateList:checked").length;
    if (ctrChecked == ctrCnt) {
        $("#cbSelectAll").prop("checked", true);
    } else {
        $("#cbSelectAll").prop("checked", false);
    }
    if (ctr.is(":checked")) {
        currTR.find("td").css("background-color", "orange");
    } else {
        if (currIdx == 0) {
            currCreateBGColor = "#eee";
        }
        currTR.find("td").css("background-color", currCreateBGColor);
    }
}

function defaultSRForm() {
    $("#srModal textarea").val(null);
    $("#srModal .rbCat").each(function () {
        $(this).prop("checked", false);
    });
    $("#selUserName,#txtDetail,.rbVia,#selITStaff,#txtJobStartDate,#txtJobEndDate,#txtJobStartTime,#txtJobEndTime").prop("disabled", false);
    $("#txtAction,#txtJobStartDate,#txtJobEndDate,#txtJobStartTime,#txtJobEndTime").val(null);
    $("#srModal .rbVia[value='4']").prop("checked", true);
    $("#srModal #selITStaff").val([]);
    $("#srModal #selITStaff").select2('destroy').select2();
    $("#srModal .modal-title").text("Add New Service Request");
    $("#btnSRSubmit").show();
    $("#btnSREdit").hide();
    curEditID = 0;
}