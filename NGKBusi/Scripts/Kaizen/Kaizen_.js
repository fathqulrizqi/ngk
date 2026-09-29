$(document).ready(function () {

    $('#KaizenDataTable').on("click", "#btnDownload", function () {
        $("#KaizenDataTable").trigger('outputTable');
    });
    if ($("#KaizenDataTable").length) {
        $("#KaizenDataTable").tablesorter({
            theme: 'metro-dark',
            widthFixed: true,
            widgets: ['zebra', 'columns', 'filter', 'stickyHeaders', 'output'],
            widgetOptions: {
                output_delivery: 'download',
                output_saveFileName: 'KaizenData.csv',
                output_replaceCR: '&amp;',
                output_replaceTab: 'asfaf',
                filter_saveFilters: true,
                filter_defaultFilter: {
                    // "{query} - a single or double quote signals an exact filter search
                    1: '"{q}',
                    5: '"{q}',
                    6: '"{q}'
                }
            }
        }).tablesorterPager({
            container: $(".pager"),
            output: '{startRow} to {endRow} ({totalRows})',
            size: 50
        });
    }

    $("#txtScore").focus(function () {
        $(this).select();
    });

    $(".rbOCDScore").change(function () {
        var Score1 = $("input[name='iOCDScore1']:checked").data("score") || 0;
        var Score2 = $("input[name='iOCDScore2']:checked").data("score") || 0;
        var Score3 = $("input[name='iOCDScore3']:checked").data("score") || 0;
        var Score4 = $("input[name='iOCDScore4']:checked").data("score") || 0;
        var Score = parseFloat(parseFloat(parseFloat(Score1) + parseFloat(Score2)) * parseFloat(parseFloat(Score3) + parseFloat(Score4)));
        $(this).closest("form").find("#txtScore").val(Score);
        $(this).closest("form").find("#txtReward").val(getReward(Score));
        if (Score < 16) {
            $(this).closest("form").find("#divReward").show("drop");
        } else {
            $(this).closest("form").find("#txtReward").val(0);
            $(this).closest("form").find("#divReward").hide("drop");
        }
    });

    $(".rbKOCScore").change(function () {
        var Score1 = $("input[name='iKOCScore1']:checked").data("score") || 0;
        var Score2 = $("input[name='iKOCScore2']:checked").data("score") || 0;
        var Score3 = $("input[name='iKOCScore3']:checked").data("score") || 0;
        var Score4 = $("input[name='iKOCScore4']:checked").data("score") || 0;
        var Score = (Score1 + Score2 + Score3 + Score4);
        $("#KOCScoreModal #txtScore").val(Score);
        var totalScore = parseFloat(OCDScore) + parseFloat(Score);

        $("#KOCScoreModal #txtReward").val(getReward(totalScore));
        if (totalScore >= 16 && totalScore < 40) {
            $("#KOCScoreModal #divReward").show("drop");
        } else {
            $("#KOCScoreModal #txtReward").val(0);
            $("#KOCScoreModal #divReward").hide("drop");
        }
    });

    $(".rbSCScore").change(function () {
        var Score1 = $("input[name='iSCScore1']:checked").data("score") || 0;
        var Score2 = $("input[name='iSCScore2']:checked").data("score") || 0;
        var Score3 = $("input[name='iSCScore3']:checked").data("score") || 0;
        var Score4 = $("input[name='iSCScore4']:checked").data("score") || 0;
        var Score = (Score1 + Score2 + Score3 + Score4);
        $("#SCScoreModal #txtScore").val(Score);
        var totalScore = parseFloat(OCDScore) + parseFloat(KOCScore) + parseFloat(Score);

        $("#SCScoreModal #txtReward").val(getReward(totalScore));
        if (totalScore >= 40) {
            $("#SCScoreModal #divReward").show("drop");
        } else {
            $("#SCScoreModal #txtReward").val(0);
            $("#SCScoreModal #divReward").hide("drop");
        }
    });

    //if ($(".txtBenefit").length > 0) {
    //    new AutoNumeric.multiple(".txtBenefit", { unformatOnSubmit: true, allowDecimalPadding: false });
    //}
    //if ($(".txtCost").length > 0) {
    //    new AutoNumeric.multiple(".txtCost", { unformatOnSubmit: true, allowDecimalPadding: false });
    //}

    $(".OCDScoreSet").click(function (e) {
        e.preventDefault();
        e.stopPropagation();
        var currTR = $(this).closest("tr");
        var currID = currTR.data("id");
        var currText = $(this).text();
        var currReward = currTR.data("reward");
        $.ajax({
            type: "POST",
            url: "/NGKBusi/Kaizen/OCD/getCurrScore",
            data: {
                iID: currID
            },
            success: function (data) {
                $("#OCDScoreModal .rbOCDScore").each(function () {
                    $(this).prop("checked", false);
                });
                if (data) {
                    var nextScore = 0;
                    var currUser = "";
                    var scoredBy = "";
                    var scoredByName = "";
                    var scoreReviser = "";
                    var kocBy = "";
                    var scBy = "";

                    var CostMaterial = "";
                    var CostServices = "";
                    var CostOtherDesc = "";
                    var CostOther = "";
                    var CostTotal = "";
                    var BenefitProductType = "";
                    var BenefitPeriod = "";
                    var BenefitQtyPcs = "";
                    var BenefitQty = "";
                    var BenefitProcessTime = "";
                    var BenefitProcess = "";
                    var BenefitOtherDesc = "";
                    var BenefitOther = "";
                    var BenefitTotal = "";
                    var CostBenefitTotal = "";



                    $.each(data, function (i, v) {
                        nextScore = v.KOCScore;
                        currUser = v.currUser;
                        scoredBy = v.ScoredBy;
                        scoredByName = v.ScoredByName;
                        scoreReviser = v.ScoredReviserName;
                        kocBy = v.KOCScoreBy;
                        scBy = v.SCScoreBy;
                        CostMaterial = v.CostMaterial;
                        CostServices = v.CostServices;
                        CostOtherDesc = v.CostOtherDesc;
                        CostOther = v.CostOther;
                        CostTotal = v.CostTotal;
                        BenefitProductType = v.BenefitProductType;
                        BenefitPeriod = v.BenefitPeriod;
                        BenefitQtyPcs = v.BenefitQtyPcs;
                        BenefitQty = v.BenefitQty;
                        BenefitProcessTime = v.BenefitProcessTime;
                        BenefitProcess = v.BenefitProcess;
                        BenefitOtherDesc = v.BenefitOtherDesc;
                        BenefitOther = v.BenefitOther;
                        BenefitTotal = v.BenefitTotal;
                        CostBenefitTotal = v.CostBenefitTotal;

                        $("#OCDScoreModal input[data-subcatID='" + v.subCatID + "']").prop("checked", true);
                    });

                    $("#OCDScoreModal #txtCostMaterial").val(CostMaterial);
                    $("#OCDScoreModal #txtCostServices").val(CostServices);
                    $("#OCDScoreModal #txtCostOtherDesc").val(CostOtherDesc);
                    $("#OCDScoreModal #txtCostOther").val(CostOther);
                    $("#OCDScoreModal #txtCostTotal").val(CostTotal);
                    $("#OCDScoreModal #txtBenefitPeriod").val(BenefitPeriod);
                    $("#OCDScoreModal #txtBenefitQtyPcs").val(BenefitQtyPcs);
                    $("#OCDScoreModal #txtBenefitQty").val(BenefitQty);
                    $("#OCDScoreModal #txtBenefitProcessTime").val(BenefitProcessTime);
                    $("#OCDScoreModal #txtBenefitProcess").val(BenefitProcess);
                    $("#OCDScoreModal #txtBenefitOtherDesc").val(BenefitOtherDesc);
                    $("#OCDScoreModal #txtBenefitOther").val(BenefitOther);
                    $("#OCDScoreModal #txtBenefitTotal").val(BenefitTotal);
                    $("#OCDScoreModal #txtCostBenefitTotal").val(CostBenefitTotal);

                    $("#OCDScoreModal #hfDataID").val(currID);
                    $("#OCDScoreModal #txtScore").val(currText);

                    if (currText < 16) {
                        $("#OCDScoreModal #divReward").show("drop");
                    } else {
                        if (CostBenefitTotal > 0) {
                            $("#OCDScoreModal #divReward").hide("drop");
                        } else {
                            $("#OCDScoreModal #divReward").show("drop");
                        }
                    }
                    if ($("#OCDScoreModal").data("allowededit") < 1 || ($("#OCDScoreModal").data("allowededit") == 1 && nextScore > 0) || ($("#OCDScoreModal").data("allowededit") == 1 && currUser != null && currUser != scoredBy) || ($("#OCDScoreModal").data("allowededit") == 2 && kocBy != null && currUser != kocBy) || ($("#OCDScoreModal").data("allowededit") == 3 && scBy != null && currUser != scBy) || currUser == null) {
                        $("#OCDScoreModal .rbOCDScore").prop("disabled", true);
                        $("#OCDScoreModal #selBenefitProductType").prop("disabled", true);
                        $("#OCDScoreModal #txtBenefitOther,#OCDScoreModal #txtBenefitOtherDesc,#OCDScoreModal #txtBenefitProcessTime,#OCDScoreModal #txtBenefitPeriod").prop("readonly", true);
                        $("#OCDScoreModal .txtCost,#OCDScoreModal #txtCostOtherDesc").prop("readonly", true);
                        $("#OCDScoreModal .modal-footer").hide();
                    } else {
                        $("#OCDScoreModal .rbOCDScore").prop("disabled", false);
                        $("#OCDScoreModal #selBenefitProductType").prop("disabled", false);
                        $("#OCDScoreModal #txtBenefitOther,#OCDScoreModal #txtBenefitOtherDesc,#OCDScoreModal #txtBenefitProcessTime,#OCDScoreModal #txtBenefitPeriod").prop("readonly", false);
                        $("#OCDScoreModal .txtCost,#OCDScoreModal #txtCostOtherDesc").prop("readonly", false);
                        $("#OCDScoreModal .modal-footer").show();
                    }
                    if ($("#OCDScoreModal").data("allowededit") > 1) {
                        $("#formOCDScore").attr("action", "/NGKBusi/Kaizen/OCD/ScoreRevise");
                    } else {
                        $("#formOCDScore").attr("action", "/NGKBusi/Kaizen/OCD/Score");
                    }
                    if (scoredBy != "" && scoredBy != null) {
                        $("#spanOCDBy").show();
                        $("#spanOCDBy span").text(scoredByName);
                    } else {
                        $("#spanOCDBy").hide();
                    }
                    if (scoreReviser != "" && scoreReviser != null) {
                        $("#spanOCDReviser").show();
                        $("#spanOCDReviser span").text(scoreReviser);
                    } else {
                        $("#spanOCDReviser").hide();
                    }
                    $("#OCDScoreModal #txtReward").val(currReward);
                }
                $("#OCDScoreModal #txtReward").val(getReward(currText));
                $("#OCDScoreModal").modal("show");
            }, error: function () {
                alert("Error Occurred, Please try again !");
            }
        });
    });


    $(".txtBenefit,.txtCost").keyup(function () {
        calculateCostBenefit();
    });

    $("#OCDScoreModal #selBenefitProductType").on('change', function () {
        calculateCostBenefitCPP();
        calculateCostBenefit();
    });

    $("#OCDScoreModal #txtBenefitQtyPcs").on('keyup', function () {
        calculateCostBenefitCPP();
        calculateCostBenefit();
    });

    $("#OCDScoreModal #txtBenefitPeriod").on('keyup', function () {
        calculateCostBenefitCPP();
        calculateCostBenefitManMinute();
        calculateCostBenefit();
    });

    $("#OCDScoreModal #txtBenefitProcessTime").on('keyup', function () {
        calculateCostBenefitManMinute();
        calculateCostBenefit();
    });

    function cbNumberCheck($data) {
        if ($.isNumeric($data)) {
            //return AutoNumeric.unformat($data);
            return $data;
        } else {
            return 0;
        }
    }

    function calculateCostBenefitCPP() {
        var currCPP = cbNumberCheck($("#OCDScoreModal #selBenefitProductType").find(':selected').attr('data-cpp'));
        var currQtyPcs = cbNumberCheck($("#OCDScoreModal #txtBenefitQtyPcs").val());
        var currBenefitPeriod = cbNumberCheck($("#OCDScoreModal #txtBenefitPeriod").val());
        if ($("#OCDScoreModal #selBenefitProductType").val() == "") {
            $("#OCDScoreModal #txtBenefitQtyPcs").prop("readonly", true);
            $("#OCDScoreModal #txtBenefitQtyPcs").val(0);
            $("#OCDScoreModal #txtBenefitQty").val(0);
        } else {
            $("#OCDScoreModal #txtBenefitQtyPcs").prop("readonly", false);
        }
        var cppTotal = (currCPP * currQtyPcs) * (22 * currBenefitPeriod);
        $("#OCDScoreModal #txtBenefitQty").val(parseInt(cppTotal));
    };


    function calculateCostBenefitManMinute() {
        var currManMinute = cbNumberCheck($("#OCDScoreModal #hfBenefitManMinute").val());
        var currProcessTime = cbNumberCheck($("#OCDScoreModal #txtBenefitProcessTime").val());
        var currBenefitPeriod = cbNumberCheck($("#OCDScoreModal #txtBenefitPeriod").val());

        var manMinuteTotal = (currManMinute * currProcessTime) * (22 * currBenefitPeriod);

        $("#OCDScoreModal #txtBenefitProcess").val(parseInt(manMinuteTotal));
    };

    function calculateCostBenefit() {
        var cost = 0;
        var benefit = 0;
        $(".txtCost").each(function () {
            var currCost = cbNumberCheck($(this).val());
            console.log(currCost)
            cost += parseFloat(currCost);
        });
        $(".txtCostTotal").val(cost);
        $(".txtBenefit").each(function () {
            var currBenefit = cbNumberCheck($(this).val());
            benefit += parseFloat(currBenefit);
        });
        $(".txtBenefitTotal").val(benefit);
        $(".txtCostBenefitTotal").val(benefit - cost);
    }

    var OCDScore = 0;
    $(".KOCScoreSet").click(function (e) {
        e.preventDefault();
        e.stopPropagation();
        var currTR = $(this).closest("tr");
        var currID = currTR.data("id");
        var currText = $(this).text();
        var currReward = currTR.data("reward");
        OCDScore = currTR.find(".OCDScoreSet").text();
        var currScope = currTR.find("td:eq(1)").text();
        var catID = 0;
        switch (currScope) {
            case "Perbaikan Produktifitas":
                catID = 6;
                break;
            case "Perbaikan Kualitas":
                catID = 5;
                break;
            case "Perbaikan K3 & Sumber Daya":
                catID = 7;
                break;
            case "Perbaikan 5S & Lingkungan":
                catID = 8;
                break;
        }
        $(".KOCScoreForm").hide();
        $.ajax({
            type: "POST",
            url: "/NGKBusi/Kaizen/KOC/getCurrScore",
            data: {
                iID: currID,
                iCatID: catID
            },
            success: function (data) {
                $("#KOCScoreModal .rbKOCScore").each(function () {
                    $(this).prop("checked", false).prop('required', false);
                });
                if (data) {
                    $("#KOCScoreModal input[data-catID='" + catID + "']").each(function () {
                        $(this).prop('required', true);
                    });
                    $(".KOCScoreForm[data-catID='" + catID + "']").show();
                    var nextScore = 0;
                    var currUser = "";
                    var scoredBy = "";
                    var scoredByName = "";
                    var scoreNote = "";
                    var scoreReviser = "";
                    var scBy = "";
                    $.each(data, function (i, v) {
                        nextScore = v.SCScore;
                        currUser = v.currUser;
                        scoredBy = v.ScoredBy;
                        scoredByName = v.ScoredByName;
                        scoreNote = v.ScoredNote;
                        scoreReviser = v.ScoredReviserName;
                        scBy = v.SCScoreBy;
                        $("#KOCScoreModal input[data-subcatID='" + v.subCatID + "']").prop("checked", true);
                    });
                    $("#KOCScoreModal #hfDataID").val(currID);
                    $("#KOCScoreModal #txtScore").val(currText);
                    if ((parseFloat(OCDScore) + parseFloat(currText)) < 40) {
                        $("#KOCScoreModal #divReward").show("drop");
                    } else {
                        $("#KOCScoreModal #divReward").hide("drop");
                    }
                    if ($("#KOCScoreModal").data("allowededit") < 1 || ($("#KOCScoreModal").data("allowededit") == 1 && nextScore > 0) || ($("#KOCScoreModal").data("allowededit") == 1 && currUser != null && scoredBy != null && currUser != scoredBy) || ($("#KOCScoreModal").data("allowededit") == 2 && scBy != null && currUser != scBy) || currUser == null) {
                        $("#KOCScoreModal .rbKOCScore").prop("disabled", true);
                        $("#txtKOCNote,#txtKOCBenefitNote").prop("readonly", true);
                        $("#KOCScoreModal .modal-footer").hide();
                    } else {
                        $("#KOCScoreModal .rbKOCScore").prop("disabled", false);
                        $("#txtKOCNote,#txtKOCBenefitNote").prop("readonly", false);
                        $("#KOCScoreModal .modal-footer").show();
                    }

                    if ($("#KOCScoreModal").data("allowededit") > 1) {
                        $("#formKOCScore").attr("action", "/NGKBusi/Kaizen/KOC/ScoreRevise");
                    } else {
                        $("#formKOCScore").attr("action", "/NGKBusi/Kaizen/KOC/Score");
                    }
                    if (scoredBy != "" && scoredBy != null) {
                        $("#spanKOCBy").show();
                        $("#spanKOCBy span").text(scoredByName);
                    } else {
                        $("#spanKOCBy").hide();
                    }
                    if (scoreReviser != "" && scoreReviser != null) {
                        $("#spanKOCReviser").show();
                        $("#spanKOCReviser span").text(scoreReviser);
                    } else {
                        $("#spanKOCReviser").hide();
                    }
                    $("#KOCScoreModal #txtReward").val(currReward);
                    $("#txtKOCNote,#txtKOCBenefitNote").val(scoreNote);
                }
                $("#KOCScoreModal").modal("show");
            }, error: function () {
                alert("Error Occurred, Please try again !");
            }
        });
    });

    var KOCScore = 0;
    $(".SCScoreSet").click(function (e) {
        e.preventDefault();
        e.stopPropagation();
        var currTR = $(this).closest("tr");
        var currID = currTR.data("id");
        var currText = $(this).text();
        var currReward = currTR.data("reward");
        OCDScore = currTR.find(".OCDScoreSet").text();
        KOCScore = currTR.find(".KOCScoreSet").text();
        var currScope = currTR.find("td:eq(1)").text();
        $.ajax({
            type: "POST",
            url: "/NGKBusi/Kaizen/SC/getCurrScore",
            data: {
                iID: currID
            },
            success: function (data) {
                $("#SCScoreModal .rbSCScore").each(function () {
                    $(this).prop("checked", false);
                });
                if (data) {
                    var currUser = "";
                    var scoredBy = "";
                    var scoredByName = "";
                    var scoreNote = "";
                    $("#SCScoreModal input[data-catID='10'],#SCScoreModal input[data-catID='11']").prop("disabled", true);
                    $.each(data, function (i, v) {
                        currUser = v.currUser;
                        scoredBy = v.ScoredBy;
                        scoredByName = v.ScoredByName;
                        scoreNote = v.ScoredNote;
                        if (v.subCatID != null) {
                            $("#SCScoreModal input[data-subcatID='" + v.subCatID + "']").prop("checked", true);
                        } else {
                            var costSubCat = v.subCatID;
                            if (v.CostTotal > 5000000) {
                                costSubCat = 37;
                            } else if (v.CostTotal > 3000000 && v.CostTotal <= 5000000) {
                                costSubCat = 38;
                            } else if (v.CostTotal > 500000 && v.CostTotal <= 3000000) {
                                costSubCat = 39;
                            } else {
                                costSubCat = 40;
                            }
                            $("#SCScoreModal input[data-subcatID='" + costSubCat + "']").prop("checked", true).prop("disabled", false);

                            var benefitSubCat = v.subCatID;
                            if (v.CostBenefitTotal > 7000000) {
                                benefitSubCat = 44;
                            } else if (v.CostBenefitTotal > 4000000 && v.CostBenefitTotal <= 7000000) {
                                benefitSubCat = 43;
                            } else if (v.CostBenefitTotal > 500000 && v.CostBenefitTotal <= 4000000) {
                                benefitSubCat = 42;
                            } else {
                                benefitSubCat = 41;
                            }
                            
                            $("#SCScoreModal input[data-subcatID='" + benefitSubCat + "']").prop("checked", true).prop("disabled", false);
                        }
                    });
                    $("#SCScoreModal #hfDataID").val(currID);
                    $("#SCScoreModal #txtScore").val(currText);
                    if ((parseFloat(OCDScore) + (parseFloat(KOCScore) + parseFloat(currText)) < 40)) {
                        $("#SCScoreModal #divReward").show("drop");
                    } else {
                        $("#SCScoreModal #divReward").hide("drop");
                    }
                    if ($("#SCScoreModal").data("allowededit") == 0 || (currUser != null && scoredBy != null && currUser != scoredBy) || currUser == null) {
                        $("#SCScoreModal .rbSCScore").prop("disabled", true);
                        $("#txtSCNote").prop("readonly", true);
                        $("#SCScoreModal .modal-footer").hide();
                    } else {
                        $("#SCScoreModal .rbSCScore:not([data-catID!='10']):not([data-catID!='11'])").prop("disabled", false);
                        $("#txtSCNote").prop("readonly", false);
                        $("#SCScoreModal .modal-footer").show();
                    }
                    if (scoredBy != "" && scoredBy != null) {
                        $("#spanSCBy").show();
                        $("#spanSCBy span").text(scoredByName);
                    } else {
                        $("#spanSCBy").hide();
                    }
                    $("#SCScoreModal #txtReward").val(currReward);
                    $("#txtSCNote").val(scoreNote);
                }
                $("#SCScoreModal").modal("show");
            }, error: function () {
                alert("Error Occurred, Please try again !");
            }
        });
    });
    $(".SCRewardSet").click(function () {
        var currTR = $(this).closest("tr");
        var currID = currTR.data("id");
        var currReward = $(this).text();

        $("#KaizenRewardModal #hfDataID").val(currID);
        $("#KaizenRewardModal #txtReward").val(currReward);
        $("#KaizenRewardModal").modal("show");
    });
    $("#selUnit").change(function () {
        var currVal = $(this).val();
        if (currVal == "Lainnya") {
            $("#txtUnitOther").fadeIn();
        } else {
            $("#txtUnitOther").fadeOut().val(null);
        }
    });
    //$(".benefitSet").click(function () {
    //    var currCtr = $(this);
    //    var currTR = currCtr.closest("tr");
    //    var currID = currTR.data("id");
    //    var currBenefit = currCtr.text();
    //    var currSaving = currCtr.data("savings");
    //    var currUnit = currCtr.data("unit");
    //    var currValue = currCtr.data("value");
    //    var currInvest = currCtr.data("invest");
    //    var currNote = currCtr.data("note");

    //    $("#txtSavings").val(currSaving);
    //    $("#txtValue").val(currValue);
    //    $("#txtInvest").val(currInvest);
    //    $("#txtKOCBenefitNote").val(currNote);

    //    if (parseInt(currBenefit, 10) > 0) {
    //        $("#txtBenefit").css("color", "green");
    //    } else {
    //        $("#txtBenefit").css("color", "red");
    //    }
    //    $("#txtBenefit").val(currBenefit);

    //    if ($("#selUnit option[value='" + currUnit + "']").length > 0) {
    //        $("#txtUnitOther").fadeOut().val(null);
    //        $("#selUnit").val(currUnit);
    //    } else {
    //        $("#txtUnitOther").fadeIn().val(currUnit);
    //        $("#selUnit").val("Lainnya");
    //    }
    //    $("#KaizenBenefitModal #hfDataID").val(currID);
    //    $("#KaizenBenefitModal").modal("show");
    //});

    //$("#txtSavings,#txtValue,#txtInvest").keyup(function () {
    //    $(this).val($(this).val().toString());
    //    var currSaving = $("#txtSavings").val() || 0;
    //    var currValue = $("#txtValue").val() || 0;
    //    var currInvest = $("#txtInvest").val() || 0;
    //    var currTotal = (parseFloat(currSaving) * parseInt(currValue)) - parseInt(currInvest);

    //    if (currTotal > 0) {
    //        $("#txtBenefit").css("color", "green");
    //    } else {
    //        $("#txtBenefit").css("color", "red");
    //    }
    //    $("#txtBenefit").val(currTotal);
    //});
    //$("#txtBenefit").keyup(function () {
    //    if ($(this).val() > 0) {
    //        $("#txtBenefit").css("color", "green");
    //    } else {
    //        $("#txtBenefit").css("color", "red");
    //    }
    //});
    $("#txtKOCNote").keyup(function () {
        $("#txtKOCBenefitNote").val($(this).val());
    });
    $("#txtKOCBenefitNote").keyup(function () {
        $("#txtKOCNote").val($(this).val());
    });

    function getReward(score) {
        var reward = 0;
        if (score >= 0 && score < 2) {
            //reward = 0;
            reward = 10000;
        } else if (score >= 2 && score <= 5) {
            //reward = 5 / 10;
            reward = 10000;
        } else if (score > 5 && score <= 12) {
            //reward = 75 / 100;
            reward = 10000;
        } else if (score > 12 && score <= 15) {
            //reward = 1;
            reward = 10000;
        } else if (score > 15 && score <= 20) {
            //reward = 5000;
            reward = 10000;
        } else if (score > 20 && score <= 24) {
            reward = 15000;
        } else if (score > 24 && score <= 28) {
            reward = 15000;
        } else if (score > 28 && score <= 32) {
            reward = 15000;
        } else if (score > 32 && score <= 36) {
            reward = 25000;
        } else if (score > 36 && score <= 40) {
            reward = 30000;
        } else if (score > 40 && score <= 44) {
            reward = 35000;
        } else if (score > 44 && score <= 52) {
            reward = 40000;
        } else if (score > 52 && score <= 60) {
            reward = 45000;
        } else if (score > 60 && score <= 68) {
            reward = 50000;
        } else if (score > 68 && score <= 76) {
            reward = 75000;
        } else if (score > 76 && score <= 84) {
            reward = 100000;
        } else if (score > 84 && score <= 92) {
            reward = 200000;
        } else if (score > 92 && score <= 100) {
            reward = 250000;
        } else {
            reward = 0;
        }
        return reward;
    }


    $(".btnLock").click(function () {
        var currCtr = $(this);
        var currTR = currCtr.closest("tr");
        var currID = currTR.data("id");
        var currRoom = currCtr.data("room");
        var currKOCScore = currTR.find("td:eq(9)").text();
        var currSCScore = currTR.find("td:eq(10)").text();
        var currScore = currKOCScore;
        var lockedBy = currCtr.data("lockedby");
        var currUser = currCtr.data("logged");
        var ajaxURL = "/NGKBusi/Kaizen/KOC/Lock";
        if (currRoom == "SC") {
            currScore = currSCScore;
            ajaxURL = "/NGKBusi/Kaizen/SC/Lock"
        }
        if (currScore == 0 && (lockedBy != 0 && currUser == lockedBy) || lockedBy == 0) {
            if (!currCtr.find("i").hasClass("fa-spinner")) {
                if (currCtr.find("i").hasClass("fa-lock")) {
                    currCtr.find("i").addClass("fa-pulse").toggleClass("fa-lock fa-spinner");
                } else {
                    currCtr.find("i").addClass("fa-pulse").toggleClass("fa-unlock fa-spinner");
                }
                $.ajax({
                    type: "POST",
                    url: ajaxURL,
                    data: {
                        iID: currID
                    },
                    success: function (data) {
                        if (!data.locked) {
                            currCtr.attr("data-lockedby", 0);
                            currCtr.toggleClass("btn-warning btn-success");
                            currCtr.find("i").removeClass("fa-pulse").toggleClass("fa-spinner fa-unlock");
                        } else {
                            currCtr.attr("data-lockedby", currUser);
                            currCtr.toggleClass("btn-success btn-warning");
                            currCtr.find("i").removeClass("fa-pulse").toggleClass("fa-spinner fa-lock");
                        }
                    }, error: function () {
                        if (currCtr.find("i").hasClass("fa-lock")) {
                            currCtr.find("i").addClass("fa-pulse").toggleClass("fa-spinner fa-lock");
                        } else {
                            currCtr.find("i").addClass("fa-pulse").toggleClass("fa-spinner fa-unlock");
                        }
                        alert("Error Occurred, Please try again !");
                    }
                });
            }
        }
    });
    $("#txtTitle").keyup(function () {
        var currVal = $(this).val();
        if (currVal.length > 0) {

            delay(function () {
                var imgLoad = '<img id="loadTitle" src="/NGKBusi/Content/Images/loading-circle-oval.gif" alt="Loading" width="45" height="45" />';
                $("#showSameResult").hide().parent().append(imgLoad);
                $.ajax({
                    type: "POST",
                    url: "/NGKBusi/Kaizen/OCD/getTitle",
                    data: {
                        currTitle: currVal
                    },
                    success: function (data) {
                        var titleCount = 0;
                        var titleLink = "";
                        $.each(data, function (i, v) {
                            titleCount++;
                            titleLink += "<tr><td class='text-center'><a href='/NGKBusi/Files/Kaizen/" + v.RegNo + ".pdf' target='_BLANK'>" + v.Title + "</a></td></tr>"
                        });
                        $("#tblTitleResult tbody").empty().append(titleLink);
                        $("#showSameResult").show().text(titleCount + " Hasil Sama");
                        $("#loadTitle").remove();
                    }, error: function () {
                        alert("Error Occurred, Please try again !");
                    }
                });
            }, 1000);
        }
    });
    $("#formKaizen #selPeriod").change(function () {
        var currVal = $(this).val();
        if (currVal != "") {
            $("#formKaizen").submit();
        }
    });

});