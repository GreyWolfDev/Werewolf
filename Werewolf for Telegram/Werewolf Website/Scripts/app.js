function FirstInit() {
    jQuery.browserDetect(), _goFullScreen(), _aside(), Init(!1)
}

function Init(e) {
    _owl_carousel(), _popover(), _lightbox(), _scrollTo(), _toggle(), _placeholder(), _charts(e), _slimScroll(), _autosuggest(), _form(), _select2(), _stepper(), _pickers(), _editors(), _misc(), _afterResize(e), _panels(), _modalAutoLoad(), _toastr(!1, !1, !1, !1), _ajaxPage(e), jQuery("[data-toggle=tooltip]").tooltip(), jQuery("body").on("click", ".dropdown-menu.hold-on-click", function (e) {
        e.stopPropagation()
    })
}

function _afterResize() {
    jQuery(window).load(function () {
        "use strict";
        jQuery(window).resize(function () {
            window.afterResizeApp && clearTimeout(window.afterResizeApp), window.afterResizeApp = setTimeout(function () {
                window.width = jQuery(window).width(), window.width > 768 ? jQuery("#mobileMenuBtn").removeClass("active") : (jQuery("#mobileMenuBtn").removeClass("active"), jQuery("body").removeClass("menu-open")), _asideFix()
            }, 300)
        })
    })
}

function _scrollTo() {
    jQuery("a.scrollTo").bind("click", function (e) {
        e.preventDefault();
        var t = jQuery(this).attr("href");
        "#" != t && jQuery("html,body").animate({
            scrollTop: jQuery(t).offset().top
        }, 800, "easeInOutExpo")
    }), jQuery("a#toTop").bind("click", function (e) {
        e.preventDefault(), jQuery("html,body").animate({
            scrollTop: 0
        }, 800, "easeInOutExpo")
    })
}

function loadScript(e, t) {
    if (_arr[e]) t && t();
    else {
        _arr[e] = !0;
        var a = document.getElementsByTagName("body")[0],
            r = document.createElement("script");
        r.type = "text/javascript", r.src = e, r.onload = t, a.appendChild(r)
    }
}

function _ajaxLink(e) {
    e === !0 && jQuery("a").bind("click", function (e) {
        if (jQuery(this).hasClass("external") || "#" == jQuery(this).attr("href") || "javascript:;" == jQuery(this).attr("href") || "javascript:void(0);" == jQuery(this).attr("href") || "javascript:void(0)" == jQuery(this).attr("href")) e.preventDefault(), e.stopPropagation();
        else {
            e.preventDefault();
            var t = jQuery(this).attr("href"),
                t = t.replace("#", "");
            window.location.hash = jQuery(this).attr("href")
        }
    })
}

function _ajaxPage(e) {
    return window.ajax ? (jQuery(document).on("click", '.ajaxNav a[href!="#"], a.ajax[href!="#"]', function (e) {
        e.preventDefault(), window.location.hash = jQuery(this).attr("href"), document.title = jQuery(this).attr("title") || jQuery(this).html().replace(/(<([^>]+)>)/gi, "")
    }), void (window.ajax === !0 && e === !1 && (jQuery(window).on("hashchange", function () {
        _loadPage(window.location.hash, "#middle")
    }), _loadPage(window.location.hash, "#middle")))) : !1
}

function _loadPage(e, t) {
    if ("#" == e) return !1;
    if (!t) var t = "#middle";
    e = e.replace("#", ""), "" == e && (window.location.hash = "#dashboard.html", e = "dashboard.html"), e = "tpl/" + e, jQuery.ajax({
        url: e,
        dataType: "html",
        type: "GET",
        cache: !0,
        async: !1,
        beforeSend: function () {
            jQuery("#middle").html('<h1 class="ajax-loading"><i class="fa fa-cog fa-spin"></i> loading</h1>')
        },
        success: function (e) {
            jQuery(t).css({
                opacity: "0.0"
            }).html(e).delay(50).animate({
                opacity: "0.0"
            }, 0, function () {
                ajaxInit(), jQuery(t).animate({
                    opacity: "1.0"
                }, 300)
            })
        },
        complete: function () {
            Init(!0)
        },
        error: function () {
            jQuery(t).html('<div class="text-center ajax-err"><h1 class="err404"><i class="fa fa-warning"></i>Page not found<small></small></h1></div>')
        }
    })
}

function _slimScroll() {
    jQuery(".slimscroll").each(function () {
        var e;
        e = jQuery(this).attr("data-height") ? jQuery(this).attr("data-height") : jQuery(this).height(), jQuery(this).slimScroll({
            size: jQuery(this).attr("data-size") || "7px",
            opacity: jQuery(this).attr("data-opacity") || .6,
            position: jQuery(this).attr("data-position") || "right",
            allowPageScroll: !1,
            disableFadeOut: !1,
            railVisible: !0,
            railColor: jQuery(this).attr("data-railColor") || "#222",
            railOpacity: jQuery(this).attr("data-railOpacity") || .05,
            alwaysVisible: "false" != jQuery(this).attr("data-alwaysVisible") ? !0 : !1,
            railVisible: "false" != jQuery(this).attr("data-railVisible") ? !0 : !1,
            color: jQuery(this).attr("data-color") || "#333",
            wrapperClass: jQuery(this).attr("data-wrapper-class") || "slimScrollDiv",
            railColor: jQuery(this).attr("data-railColor") || "#eaeaea",
            height: e
        }), "true" == jQuery(this).attr("disable-body-scroll") && jQuery(this).bind("mousewheel DOMMouseScroll", function (e) {
            var t = null;
            "mousewheel" == e.type ? t = -1 * e.originalEvent.wheelDelta : "DOMMouseScroll" == e.type && (t = 40 * e.originalEvent.detail), t && (e.preventDefault(), jQuery(this).scrollTop(t + jQuery(this).scrollTop()))
        })
    })
}

function _owl_carousel() {
    if (!jQuery().owlCarousel) return !1;
    var total = jQuery("div.owl-carousel").length,
        count = 0;
    jQuery("div.owl-carousel").each(function () {
        function progressBar(e) {
            $elem = e, buildProgressBar(), start()
        }

        function buildProgressBar() {
            $progressBar = $("<div>", {
                id: "progressBar"
            }), $bar = $("<div>", {
                id: "bar"
            }), $progressBar.append($bar).prependTo($elem)
        }

        function start() {
            percentTime = 0, isPause = !1, tick = setInterval(interval, 10)
        }

        function interval() {
            isPause === !1 && (percentTime += 1 / time, $bar.css({
                width: percentTime + "%"
            }), percentTime >= 100 && $elem.trigger("owl.next"))
        }

        function pauseOnDragging() {
            isPause = !0
        }

        function moved() {
            clearTimeout(tick), start()
        }
        var slider = jQuery(this),
            options = slider.attr("data-plugin-options"),
            $opt = eval("(" + options + ")");
        if ("true" == $opt.progressBar) var afterInit = progressBar;
        else var afterInit = !1;
        var defaults = {
            items: 5,
            itemsCustom: !1,
            itemsDesktop: [1199, 4],
            itemsDesktopSmall: [980, 3],
            itemsTablet: [768, 2],
            itemsTabletSmall: !1,
            itemsMobile: [479, 1],
            singleItem: !0,
            itemsScaleUp: !1,
            slideSpeed: 200,
            paginationSpeed: 800,
            rewindSpeed: 1e3,
            autoPlay: !1,
            stopOnHover: !1,
            navigation: !1,
            navigationText: ['<i class="fa fa-chevron-left"></i>', '<i class="fa fa-chevron-right"></i>'],
            rewindNav: !0,
            scrollPerPage: !1,
            pagination: !0,
            paginationNumbers: !1,
            responsive: !0,
            responsiveRefreshRate: 200,
            responsiveBaseWidth: window,
            baseClass: "owl-carousel",
            theme: "owl-theme",
            lazyLoad: !1,
            lazyFollow: !0,
            lazyEffect: "fade",
            autoHeight: !1,
            jsonPath: !1,
            jsonSuccess: !1,
            dragBeforeAnimFinish: !0,
            mouseDrag: !0,
            touchDrag: !0,
            transitionStyle: !1,
            addClassActive: !1,
            beforeUpdate: !1,
            afterUpdate: !1,
            beforeInit: !1,
            afterInit: afterInit,
            beforeMove: !1,
            afterMove: 0 == afterInit ? !1 : moved,
            afterAction: !1,
            startDragging: !1,
            afterLazyLoad: !1
        },
            config = jQuery.extend({}, defaults, options, slider.data("plugin-options"));
        slider.owlCarousel(config).addClass("owl-carousel-init");
        var elem = jQuery(this),
            time = 7
    })
}


function _popover() {
    jQuery("a[data-toggle=popover]").bind("click", function (e) {
        jQuery(".popover-title .close").remove(), e.preventDefault()
    });
    var e = !1,
        t = !1;
    jQuery("a[data-toggle=popover], button[data-toggle=popover]").popover({
        html: !0,
        trigger: "manual"
    }).click(function (a) {
        jQuery(this).popover("show"), t = !1, e = !0, a.preventDefault()
    }), jQuery(document).click(function () {
        e & t ? (jQuery("a[data-toggle=popover], button[data-toggle=popover]").popover("hide"), e = t = !1) : t = !0
    }), jQuery("a[data-toggle=popover], button[data-toggle=popover]").popover({
        html: !0,
        trigger: "manual"
    }).click(function (e) {
        $(this).popover("show"), $(".popover-title").append('<button type="button" class="close">&times;</button>'), $(".close").click(function () {
            jQuery("a[data-toggle=popover], button[data-toggle=popover]").popover("hide")
        }), e.preventDefault()
    })
}

function _lightbox() {
    var e = jQuery(".lightbox");
    e.length > 0 && loadScript(plugin_path + "magnific-popup/jquery.magnific-popup.min.js", function () {
        return "undefined" == typeof jQuery.magnificPopup ? !1 : (jQuery.extend(!0, jQuery.magnificPopup.defaults, {
            tClose: "Close",
            tLoading: "Loading...",
            gallery: {
                tPrev: "Previous",
                tNext: "Next",
                tCounter: "%curr% / %total%"
            },
            image: {
                tError: "Image not loaded!"
            },
            ajax: {
                tError: "Content not loaded!"
            }
        }), void e.each(function () {
            var e = jQuery(this),
                t = e.attr("data-plugin-options"),
                a = {},
                r = {
                    type: "image",
                    fixedContentPos: !1,
                    fixedBgPos: !1,
                    mainClass: "mfp-no-margins mfp-with-zoom",
                    closeOnContentClick: !0,
                    closeOnBgClick: !0,
                    image: {
                        verticalFit: !0
                    },
                    zoom: {
                        enabled: !1,
                        duration: 300
                    },
                    gallery: {
                        enabled: !1,
                        navigateByImgClick: !0,
                        preload: [0, 1],
                        arrowMarkup: '<button title="%title%" type="button" class="mfp-arrow mfp-arrow-%dir%"></button>',
                        tPrev: "Previous",
                        tNext: "Next",
                        tCounter: '<span class="mfp-counter">%curr% / %total%</span>'
                    }
                };
            e.data("plugin-options") && (a = jQuery.extend({}, r, t, e.data("plugin-options"))), jQuery(this).magnificPopup(a)
        }))
    })
}

function _toggle() {
    var e = 25;
    jQuery("div.toggle.active > p").addClass("preview-active"), jQuery("div.toggle.active > div.toggle-content").slideDown(400), jQuery("div.toggle > label").click(function (t) {
        var a = jQuery(this).parent(),
            r = jQuery(this).parents("div.toggle"),
            i = !1,
            n = r.hasClass("toggle-accordion");
        if (n && "undefined" != typeof t.originalEvent && r.find("div.toggle.active > label").trigger("click"), a.toggleClass("active"), a.find("> p").get(0)) {
            i = a.find("> p");
            var o = i.css("height"),
                l = i.css("height");
            i.css("height", "auto"), i.css("height", o)
        }
        var s = a.find("> div.toggle-content");
        a.hasClass("active") ? (jQuery(i).animate({
            height: l
        }, 350, function () {
            jQuery(this).addClass("preview-active")
        }), s.slideDown(350)) : (jQuery(i).animate({
            height: e
        }, 350, function () {
            jQuery(this).removeClass("preview-active")
        }), s.slideUp(350))
    })
}

function _charts(e) {
    jQuery(".sparkline").length > 0 && loadScript(plugin_path + "chart.sparkline/jquery.sparkline.min.js", function () {
        jQuery().sparkline && (e === !0 ? jQuery("#middle .sparkline").each(function () {
            jQuery(this).sparkline("html", jQuery(this).data("plugin-options"))
        }) : jQuery(".sparkline").each(function () {
            jQuery(this).sparkline("html", jQuery(this).data("plugin-options"))
        }))
    }), jQuery(".easyPieChart").length > 0 && loadScript(plugin_path + "chart.easypiechart/jquery.easypiechart.min.js", function () {
        jQuery().easyPieChart && jQuery(".easyPieChart").each(function () {
            var e = jQuery(this).attr("data-size") || "110";
            jQuery(this).width(e), jQuery(this).height(e), jQuery(this).easyPieChart({
                easing: jQuery(this).attr("data-easing") || "",
                barColor: jQuery(this).attr("data-barColor") || "#ef1e25",
                trackColor: jQuery(this).attr("data-trackColor") || "#dddddd",
                scaleColor: jQuery(this).attr("data-scaleColor") || "#dddddd",
                size: jQuery(this).attr("data-size") || "110",
                lineWidth: jQuery(this).attr("data-lineWidth") || "6",
                lineCap: "circle",
                onStep: function (e, t, a) {
                    jQuery(this.el).find(".percent").text(Math.round(a))
                }
            })
        })
    }), jQuery("input.knob").length > 0 && loadScript(plugin_path + "chart.knob/dist/jquery.knob.min.js", function () {
        jQuery().knob && jQuery("input.knob").knob({
            dynamicDraw: !0,
            thickness: jQuery(this).attr("data-thickness") || .1,
            tickColorizeValues: !0,
            skin: "tron"
        })
    })
}

function _autosuggest() {
    _container = jQuery("div.autosuggest"), _container.length > 0 && loadScript(plugin_path + "typeahead.bundle.js", function () {
        jQuery().typeahead && _container.each(function () {
            var e = jQuery(this),
                t = e.attr("data-minLength") || 1,
                a = e.attr("data-queryURL"),
                r = e.attr("data-limit") || 10,
                i = e.attr("data-autoload");
            if ("false" == i) return !1;
            var n = new Bloodhound({
                datumTokenizer: Bloodhound.tokenizers.obj.whitespace("value"),
                queryTokenizer: Bloodhound.tokenizers.whitespace,
                limit: r,
                remote: {
                    url: a + "%QUERY"
                }
            });
            jQuery(".typeahead", e).typeahead({
                limit: r,
                hint: "false" == e.attr("data-hint") ? !1 : !0,
                highlight: "false" == e.attr("data-highlight") ? !1 : !0,
                minLength: parseInt(t),
                cache: !1
            }, {
                name: "_typeahead",
                source: n
            })
        })
    })
}

function _form() {
    jQuery("form.validate-plugin").length > 0 && loadScript(plugin_path + "form.validate/jquery.form.min.js", function () {
        loadScript(plugin_path + "form.validate/jquery.validation.min.js")
    }), jQuery("form.validate").length > 0 && loadScript(plugin_path + "form.validate/jquery.form.min.js", function () {
        loadScript(plugin_path + "form.validate/jquery.validation.min.js", function () {
            jQuery().validate && jQuery("form.validate").each(function () {
                var e = jQuery(this),
                    t = e.attr("data-success") || "Successfully! Thank you!",
                    a = (e.attr("data-captcha") || "Invalid Captcha!", e.attr("data-toastr-position") || "top-right"),
                    r = e.attr("data-toastr-type") || "success";
                _Turl = e.attr("data-toastr-url") || !1, e.append('<input type="hidden" name="is_ajax" value="true" />'), e.validate({
                    submitHandler: function (e) {
                        jQuery(e).find(".input-group-addon").find(".fa-envelope").removeClass("fa-envelope").addClass("fa-refresh fa-spin"), jQuery(e).ajaxSubmit({
                            target: jQuery(e).find(".validate-result").length > 0 ? jQuery(e).find(".validate-result") : "",
                            error: function () {
                                _toastr("Sent Failed!", a, "error", !1)
                            },
                            success: function (i) {
                                var i = i.trim();
                                "_failed_" == i ? _toastr("SMTP ERROR! Please, check your config file!", a, "error", !1) : "_captcha_" == i ? _toastr("Invalid Captcha!", a, "error", !1) : (jQuery(e).find(".input-group-addon").find(".fa-refresh").removeClass("fa-refresh fa-spin").addClass("fa-envelope"), jQuery(e).find("input.form-control").val(""), _toastr(t, a, r, _Turl))
                            }
                        })
                    }
                })
            })
        })
    });
    var e = jQuery("input.masked");
    e.length > 0 && loadScript(plugin_path + "form.masked/jquery.maskedinput.js", function () {
        e.each(function () {
            var e = jQuery(this);
            _format = e.attr("data-format") || "(999) 999-999999", _placeholder = e.attr("data-placeholder") || "X", jQuery.mask.definitions.f = "[A-Fa-f0-9]", e.mask(_format, {
                placeholder: _placeholder
            })
        })
    })
}

function _select2() {
    var e = jQuery("select.select2");
    e.length > 0 && loadScript(plugin_path + "select2/js/select2.full.min.js", function () {
        jQuery().select2 && jQuery("select.select2").select2()
    })
}

function _stepper() {
    var e = jQuery("input.stepper");
    e.length > 0 && loadScript(plugin_path + "form.stepper/jquery.stepper.min.js", function () {
        jQuery().stepper && jQuery(e).each(function () {
            var e = jQuery(this),
                t = e.attr("min") || null,
                a = e.attr("max") || null;
            e.stepper({
                limit: [t, a],
                floatPrecission: e.attr("data-floatPrecission") || 2,
                wheel_step: e.attr("data-wheelstep") || .1,
                arrow_step: e.attr("data-arrowstep") || .2,
                allowWheel: "false" == e.attr("data-mousescrool") ? !1 : !0,
                UI: "false" == e.attr("data-UI") ? !1 : !0,
                type: e.attr("data-type") || "float",
                preventWheelAcceleration: "false" == e.attr("data-preventWheelAcceleration") ? !1 : !0,
                incrementButton: e.attr("data-incrementButton") || "&blacktriangle;",
                decrementButton: e.attr("data-decrementButton") || "&blacktriangledown;",
                onStep: null,
                onWheel: null,
                onArrow: null,
                onButton: null,
                onKeyUp: null
            })
        })
    })
}

function _pickers() {
    var e = jQuery(".datepicker");
    e.length > 0 && loadScript(plugin_path + "bootstrap.datepicker/js/bootstrap-datepicker.min.js", function () {
        jQuery().datepicker && e.each(function () {
            var e = jQuery(this),
                t = e.attr("data-lang") || "en";
            "en" != t && "" != t && loadScript(plugin_path + "bootstrap.datepicker/locales/bootstrap-datepicker." + t + ".min.js"), jQuery(this).datepicker({
                format: e.attr("data-format") || "yyyy-mm-dd",
                language: t,
                rtl: "true" == e.attr("data-RTL") ? !0 : !1,
                changeMonth: "false" == e.attr("data-changeMonth") ? !1 : !0,
                todayBtn: "false" == e.attr("data-todayBtn") ? !1 : "linked",
                calendarWeeks: "false" == e.attr("data-calendarWeeks") ? !1 : !0,
                autoclose: "false" == e.attr("data-autoclose") ? !1 : !0,
                todayHighlight: "false" == e.attr("data-todayHighlight") ? !1 : !0,
                onRender: function () { }
            }).on("changeDate", function () { }).data("datepicker")
        })
    });
    var t = jQuery(".rangepicker");
    t.length > 0 && loadScript(plugin_path + "bootstrap.daterangepicker/moment.min.js", function () {
        loadScript(plugin_path + "bootstrap.daterangepicker/daterangepicker.js", function () {
            jQuery().datepicker && t.each(function () {
                var e = jQuery(this),
                    t = e.attr("data-format").toUpperCase() || "YYYY-MM-DD";
                e.daterangepicker({
                    format: t,
                    startDate: e.attr("data-from"),
                    endDate: e.attr("data-to"),
                    ranges: {
                        Today: [moment(), moment()],
                        Yesterday: [moment().subtract(1, "days"), moment().subtract(1, "days")],
                        "Last 7 Days": [moment().subtract(6, "days"), moment()],
                        "Last 30 Days": [moment().subtract(29, "days"), moment()],
                        "This Month": [moment().startOf("month"), moment().endOf("month")],
                        "Last Month": [moment().subtract(1, "month").startOf("month"), moment().subtract(1, "month").endOf("month")]
                    }
                }, function () { })
            })
        })
    });
    var a = jQuery(".timepicker");
    a.length > 0 && loadScript(plugin_path + "timepicki/timepicki.min.js", function () {
        jQuery().timepicki && a.timepicki()
    });
    var r = jQuery(".colorpicker");
    r.length > 0 && loadScript(plugin_path + "spectrum/spectrum.min.js", function () {
        jQuery().spectrum && r.each(function () {
            var e = jQuery(this),
                t = e.attr("data-format") || "hex",
                a = e.attr("data-palletteOnly") || "false",
                r = e.attr("data-fullpicker") || "false",
                i = e.attr("data-allowEmpty") || !1;
            if (_flat = e.attr("data-flat") || !1, "true" == a || "true" == r) var n = [
                ["#000", "#444", "#666", "#999", "#ccc", "#eee", "#f3f3f3", "#fff"],
                ["#f00", "#f90", "#ff0", "#0f0", "#0ff", "#00f", "#90f", "#f0f"],
                ["#f4cccc", "#fce5cd", "#fff2cc", "#d9ead3", "#d0e0e3", "#cfe2f3", "#d9d2e9", "#ead1dc"],
                ["#ea9999", "#f9cb9c", "#ffe599", "#b6d7a8", "#a2c4c9", "#9fc5e8", "#b4a7d6", "#d5a6bd"],
                ["#e06666", "#f6b26b", "#ffd966", "#93c47d", "#76a5af", "#6fa8dc", "#8e7cc3", "#c27ba0"],
                ["#c00", "#e69138", "#f1c232", "#6aa84f", "#45818e", "#3d85c6", "#674ea7", "#a64d79"],
                ["#900", "#b45f06", "#bf9000", "#38761d", "#134f5c", "#0b5394", "#351c75", "#741b47"],
                ["#600", "#783f04", "#7f6000", "#274e13", "#0c343d", "#073763", "#20124d", "#4c1130"]
            ];
            else n = null;
            _color = e.attr("data-defaultColor") ? e.attr("data-defaultColor") : "#ff0000", e.attr("data-defaultColor") || "true" != i || (_color = null), e.spectrum({
                showPaletteOnly: "true" == a ? !0 : !1,
                togglePaletteOnly: "true" == a ? !0 : !1,
                flat: "true" == _flat ? !0 : !1,
                showInitial: "true" == i ? !0 : !1,
                showInput: "true" == i ? !0 : !1,
                allowEmpty: "true" == i ? !0 : !1,
                chooseText: e.attr("data-chooseText") || "Coose",
                cancelText: e.attr("data-cancelText") || "Cancel",
                color: _color,
                showInput: !0,
                showPalette: !0,
                preferredFormat: t,
                showAlpha: "rgb" == t ? !0 : !1,
                palette: n
            })
        })
    })
}

function _editors() {
    var e = jQuery("textarea.summernote");
    e.length > 0 && loadScript(plugin_path + "editor.summernote/summernote.min.js", function () {
        jQuery().summernote && e.each(function () {
            var e = jQuery(this).attr("data-lang") || "en-US";
            "en-US" != e && (alert(e), loadScript(plugin_path + "editor.summernote/lang/summernote-" + e + ".js")), jQuery(this).summernote({
                height: jQuery(this).attr("data-height") || 200,
                lang: jQuery(this).attr("data-lang") || "en-US",
                toolbar: [
                    ["style", ["style"]],
                    ["fontsize", ["fontsize"]],
                    ["style", ["bold", "italic", "underline", "strikethrough", "clear"]],
                    ["color", ["color"]],
                    ["para", ["ul", "ol", "paragraph"]],
                    ["table", ["table"]],
                    ["media", ["link", "picture", "video"]],
                    ["misc", ["codeview", "fullscreen", "help"]]
                ]
            })
        })
    });
    var t = jQuery("textarea.markdown");
    t.length > 0 && loadScript(plugin_path + "editor.markdown/js/bootstrap-markdown.min.js", function () {
        jQuery().markdown && t.each(function () {
            var e = jQuery(this),
                t = e.attr("data-lang") || "en";
            "en" != t && loadScript(plugin_path + "editor.markdown/locale/bootstrap-markdown." + t + ".js"), jQuery(this).markdown({
                autofocus: "true" == e.attr("data-autofocus") ? !0 : !1,
                savable: "true" == e.attr("data-savable") ? !0 : !1,
                height: e.attr("data-height") || "inherit",
                language: "en" == t ? null : t
            })
        })
    })
}

function _misc() {
    jQuery().masonry && jQuery(".masonry").masonry(), jQuery(".incr").bind("click", function (e) {
        e.preventDefault();
        var t = jQuery(this).attr("data-for"),
            a = parseInt(jQuery(this).attr("data-max")),
            r = parseInt(jQuery("#" + t).val());
        a > r && jQuery("#" + t).val(r + 1)
    }), jQuery(".decr").bind("click", function (e) {
        e.preventDefault();
        var t = jQuery(this).attr("data-for"),
            a = parseInt(jQuery(this).attr("data-min")),
            r = parseInt(jQuery("#" + t).val());
        r > a && jQuery("#" + t).val(r - 1)
    }), jQuery("a.toggle-default").bind("click", function (e) {
        e.preventDefault();
        var t = jQuery(this).attr("href");
        jQuery(t).is(":hidden") ? (jQuery(t).slideToggle(200), jQuery("i.fa", this).removeClass("fa-plus-square").addClass("fa-minus-square")) : (jQuery(t).slideToggle(200), jQuery("i.fa", this).removeClass("fa-minus-square").addClass("fa-plus-square"))
    });
    var e = jQuery("input[type=file]");
    e.length > 0 && loadScript(plugin_path + "custom.fle_upload.js"), jQuery("textarea.word-count").on("keyup", function () {
        var e = jQuery(this),
            t = this.value.match(/\S+/g).length,
            a = e.attr("data-maxlength") || 200;
        if (t > parseInt(a)) {
            var r = e.val().split(/\s+/, 200).join(" ");
            e.val(r + " ")
        } else {
            var i = e.attr("data-info");
            if ("" == i || void 0 == i) {
                var n = e.next("div");
                jQuery("span", n).text(t + "/" + a)
            } else jQuery("#" + i).text(t + "/" + a)
        }
    })
}

function _goFullScreen() {
    jQuery("#goToFullScreen").unbind(), jQuery("#goToFullScreen").bind("click", function (e) {
        e.preventDefault(), document.fullscreenElement || document.mozFullScreenElement || document.webkitFullscreenElement ? document.cancelFullScreen ? document.cancelFullScreen() : document.mozCancelFullScreen ? document.mozCancelFullScreen() : document.webkitCancelFullScreen && document.webkitCancelFullScreen() : document.documentElement.requestFullscreen ? document.documentElement.requestFullscreen() : document.documentElement.mozRequestFullScreen ? document.documentElement.mozRequestFullScreen() : document.documentElement.webkitRequestFullscreen && document.documentElement.webkitRequestFullscreen(Element.ALLOW_KEYBOARD_INPUT)
    })
}

function _placeholder() {
    -1 != navigator.appVersion.indexOf("MSIE") && jQuery("[placeholder]").focus(function () {
        var e = jQuery(this);
        e.val() == e.attr("placeholder") && (e.val(""), e.removeClass("placeholder"))
    }).blur(function () {
        var e = jQuery(this);
        ("" == e.val() || e.val() == e.attr("placeholder")) && (e.addClass("placeholder"), e.val(e.attr("placeholder")))
    }).blur()
}

function _aside() {
    jQuery("#mobileMenuBtn").bind("click", function (e) {
        e.preventDefault(), jQuery(this).toggleClass("active"), window.width > 768 ? jQuery("body").hasClass("min") ? (jQuery("body").removeClass("min"), jQuery("#sideNav>h3").show(), jQuery("#middle").css({
            "margin-left": ""
        })) : (jQuery("#middle").css({
            "margin-left": "0"
        }), jQuery("body").addClass("min"), jQuery("#aside nav li.el_primary.menu-open ul.sub-menu").prop("style") && jQuery("#aside nav li.el_primary.menu-open ul.sub-menu").prop("style").removeProperty("display"), jQuery("#sideNav>h3").hide(), jQuery("#aside nav li.el_primary").removeClass("menu-open")) : jQuery("body").hasClass("menu-open") ? (jQuery("body").removeClass("menu-open"), jQuery("#sideNav>h3").show(), jQuery("#middle").css({
            "margin-left": ""
        })) : (jQuery("#middle").css({
            "margin-left": "0"
        }), jQuery("body").addClass("menu-open"), jQuery("#aside nav li.el_primary.menu-open ul.sub-menu").prop("style") && jQuery("#aside nav li.el_primary.menu-open ul.sub-menu").prop("style").removeProperty("display"), jQuery("#sideNav>h3").show(), jQuery("#aside nav li.el_primary").removeClass("menu-open"))
    }), count = 0, jQuery("#aside ul.nav > li").each(function () {
        jQuery(this).addClass("el_primary"), jQuery(this).attr("id", "el_" + count), count++
    }), jQuery("#aside ul.nav li a").bind("click", function (e) {
        var t = jQuery(this),
            a = t.attr("href");
        "#" == a && e.preventDefault();
        var r = jQuery(this).closest("li");
        if (!r.hasClass("always-open")) {
            var i = r.attr("id");
            r.hasClass("el_primary") && jQuery("#aside ul.nav li>ul").each(function () {
                var e = jQuery(this).closest("li").attr("id");
                e != i && jQuery(this).slideUp(200, function () {
                    jQuery(this).parent().removeClass("menu-open")
                })
            }), jQuery(this).next().slideToggle(200, function () {
                jQuery(this).is(":visible") ? r.addClass("menu-open") : r.removeClass("menu-open active")
            })
        }
    })
}

function _asideFix() {
    window.width > 768 && jQuery("body").hasClass("menu-open") && jQuery("#middle").css({
        "margin-left": ""
    })
}

function _panels() {
    jQuery("#middle div.panel ul.options>li>a.panel_colapse").bind("click", function (e) {
        e.preventDefault();
        var t = jQuery(this).closest("div.panel"),
            a = jQuery(this),
            r = jQuery("div.panel-body", t),
            i = jQuery("div.panel-footer", t),
            n = t.attr("id");
        i.slideToggle(200), r.slideToggle(200, function () {
            r.is(":hidden") ? "" != n && void 0 != n && localStorage.setItem(n, "hidden") : localStorage.removeItem(n)
        }), a.toggleClass("plus").toggleClass("")
    }), jQuery("#middle div.panel").each(function () {
        var e = jQuery(this),
            t = jQuery("div.panel-body", e),
            a = jQuery("div.panel-footer", e),
            r = e.attr("id"),
            i = localStorage.getItem(r),
            n = jQuery("a.panel_colapse", e);
        "hidden" == i && (t.slideToggle(0), a.slideToggle(0), n.toggleClass("plus").toggleClass("")), "removed" == i && jQuery("#" + r).remove()
    }), jQuery("#middle div.panel ul.options>li>a.panel_close").bind("click", function (e) {
        e.preventDefault();
        var t = jQuery(this).closest("div.panel"),
            a = t.attr("id");
        jQuery(t).fadeOut(300, function () {
            jQuery(this).remove(), "function" == typeof _closePanel && _closePanel(a)
        })
    }), jQuery("#middle div.panel ul.options>li>a.panel_fullscreen").bind("click", function (e) {
        e.preventDefault();
        var t = jQuery(this).closest("div.panel"),
            a = jQuery("a.panel_close", t).closest("li");
        panel_colapse = jQuery("a.panel_colapse", t).closest("li"), t.toggleClass("fullscreen").toggleClass(""), a.toggleClass("hide").toggleClass(""), panel_colapse.toggleClass("hide").toggleClass(""), t.hasClass("fullscreen") ? disable_scroll() : enable_scroll()
    }), document.onkeydown = function (e) {
        if (e = e || window.event, 27 == e.keyCode) {
            var t = jQuery("#middle div.panel.fullscreen");
            t.length > 0 && (panel_close = jQuery("a.panel_close", t).closest("li"), panel_colapse = jQuery("a.panel_colapse", t).closest("li"), panel_close.removeClass("hide"), panel_colapse.removeClass("hide"), t.removeClass("fullscreen"))
        }
    }
}

function _modalAutoLoad() {
    jQuery("div.modal").length > 0 && jQuery("div.modal").each(function () {
        var e = jQuery(this),
            t = e.attr("id"),
            a = e.attr("data-autoload") || !1;
        "" != t && "hidden" == localStorage.getItem(t) && (a = "false"), "true" == a && jQuery(window).load(function () {
            var t = e.attr("data-autoload-delay") || 1e3;
            setTimeout(function () {
                e.modal("toggle")
            }, parseInt(t))
        }), jQuery("input.loadModalHide", this).bind("click", function () {
            var e = jQuery(this);
            e.is(":checked") ? (localStorage.setItem(t, "hidden"), console.log("[Modal Autoload #" + t + "] Added to localStorage")) : (localStorage.removeItem(t), console.log("[Modal Autoload #" + t + "] Removed from localStorage"))
        })
    })
}

function _toastr(e, t, a, r) {
    var i = jQuery(".toastr-notify");
    (i.length > 0 || 0 != e) && loadScript(plugin_path + "toastr/toastr.js", function () {
        i.bind("click", function (e) {
            e.preventDefault();
            var t = jQuery(this).attr("data-message"),
                a = jQuery(this).attr("data-notifyType") || "default",
                r = jQuery(this).attr("data-position") || "top-right",
                i = "true" == jQuery(this).attr("data-progressBar") ? !0 : !1,
                n = "true" == jQuery(this).attr("data-closeButton") ? !0 : !1,
                o = "true" == jQuery(this).attr("data-debug") ? !0 : !1,
                l = "true" == jQuery(this).attr("data-newestOnTop") ? !0 : !1,
                s = "true" == jQuery(this).attr("data-preventDuplicates") ? !0 : !1,
                u = jQuery(this).attr("data-showDuration") || "300",
                c = jQuery(this).attr("data-hideDuration") || "1000",
                d = jQuery(this).attr("data-timeOut") || "5000",
                p = jQuery(this).attr("data-extendedTimeOut") || "1000",
                h = jQuery(this).attr("data-showEasing") || "swing",
                f = jQuery(this).attr("data-hideEasing") || "linear",
                m = jQuery(this).attr("data-showMethod") || "fadeIn",
                y = jQuery(this).attr("data-hideMethod") || "fadeOut";
            toastr.options = {
                closeButton: n,
                debug: o,
                newestOnTop: l,
                progressBar: i,
                positionClass: "toast-" + r,
                preventDuplicates: s,
                onclick: null,
                showDuration: u,
                hideDuration: c,
                timeOut: d,
                extendedTimeOut: p,
                showEasing: h,
                hideEasing: f,
                showMethod: m,
                hideMethod: y
            }, toastr[a](t)
        }), 0 != e && (onclick = 0 != r ? function () {
            window.location = r
        } : null, toastr.options = {
            closeButton: !0,
            debug: !1,
            newestOnTop: !1,
            progressBar: !0,
            positionClass: "toast-" + t,
            preventDuplicates: !1,
            onclick: onclick,
            showDuration: "300",
            hideDuration: "1000",
            timeOut: "5000",
            extendedTimeOut: "1000",
            showEasing: "swing",
            hideEasing: "linear",
            showMethod: "fadeIn",
            hideMethod: "fadeOut"
        }, setTimeout(function () {
            toastr[a](e)
        }, 1500))
    })
}

function wheel(e) {
    e.preventDefault()
}

function disable_scroll() {
    window.addEventListener && window.addEventListener("DOMMouseScroll", wheel, !1), window.onmousewheel = document.onmousewheel = wheel
}

function enable_scroll() {
    window.removeEventListener && window.removeEventListener("DOMMouseScroll", wheel, !1), window.onmousewheel = document.onmousewheel = document.onkeydown = null
}

function enable_overlay() {
    jQuery("span.global-overlay").remove(), jQuery("body").append('<span class="global-overlay"></span>')
}

function disable_overlay() {
    jQuery("span.global-overlay").remove()
}
window.width = jQuery(window).width(), jQuery(window).ready(function () {
    loadScript(plugin_path + "bootstrap/js/bootstrap.min.js", function () {
        FirstInit()
    })
});
var _arr = {};
! function (e) {
    e.extend({
        browserDetect: function () {
            var e = navigator.userAgent,
                t = e.toLowerCase(),
                a = function (e) {
                    return t.indexOf(e) > -1
                },
                r = "gecko",
                i = "webkit",
                n = "safari",
                o = "opera",
                l = document.documentElement,
                s = [!/opera|webtv/i.test(t) && /msie\s(\d)/.test(t) ? "ie ie" + parseFloat(navigator.appVersion.split("MSIE")[1]) : a("firefox/2") ? r + " ff2" : a("firefox/3.5") ? r + " ff3 ff3_5" : a("firefox/3") ? r + " ff3" : a("gecko/") ? r : a("opera") ? o + (/version\/(\d+)/.test(t) ? " " + o + RegExp.jQuery1 : /opera(\s|\/)(\d+)/.test(t) ? " " + o + RegExp.jQuery2 : "") : a("konqueror") ? "konqueror" : a("chrome") ? i + " chrome" : a("iron") ? i + " iron" : a("applewebkit/") ? i + " " + n + (/version\/(\d+)/.test(t) ? " " + n + RegExp.jQuery1 : "") : a("mozilla/") ? r : "", a("j2me") ? "mobile" : a("iphone") ? "iphone" : a("ipod") ? "ipod" : a("mac") ? "mac" : a("darwin") ? "mac" : a("webtv") ? "webtv" : a("win") ? "win" : a("freebsd") ? "freebsd" : a("x11") || a("linux") ? "linux" : "", "js"];
            c = s.join(" "), l.className += " " + c;
            var u = !window.ActiveXObject && "ActiveXObject" in window;
            return u ? void jQuery("html").removeClass("gecko").addClass("ie ie11") : void 0
        }
    })
}(jQuery),
function (e, t, a) {
    function r() {
        e(this).find(u).each(n)
    }

    function i(e) {
        for (var e = e.attributes, t = {}, a = /^jQuery\d+/, r = 0; r < e.length; r++) e[r].specified && !a.test(e[r].name) && (t[e[r].name] = e[r].value);
        return t
    }

    function n() {
        var t, a = e(this);
        a.is(":password") || (a.data("password") ? (t = a.next().show().focus(), e("label[for=" + a.attr("id") + "]").attr("for", t.attr("id")), a.remove()) : a.realVal() == a.attr("placeholder") && (a.val(""), a.removeClass("placeholder")))
    }

    function o() {
        var t, a, r = e(this);
        placeholder = r.attr("placeholder"), e.trim(r.val()).length > 0 || (r.is(":password") ? (a = r.attr("id") + "-clone", t = e("<input/>").attr(e.extend(i(this), {
            type: "text",
            value: placeholder,
            "data-password": 1,
            id: a
        })).addClass("placeholder"), r.before(t).hide(), e("label[for=" + r.attr("id") + "]").attr("for", a)) : (r.val(placeholder), r.addClass("placeholder")))
    }
    var l = "placeholder" in t.createElement("input"),
        s = "placeholder" in t.createElement("textarea"),
        u = ":input[placeholder]";
    e.placeholder = {
        input: l,
        textarea: s
    }, !a && l && s ? e.fn.placeholder = function () { } : (!a && l && !s && (u = "textarea[placeholder]"), e.fn.realVal = e.fn.val, e.fn.val = function () {
        var t, a = e(this);
        return arguments.length > 0 ? a.realVal.apply(this, arguments) : (t = a.realVal(), a = a.attr("placeholder"), t == a ? "" : t)
    }, e.fn.placeholder = function () {
        return this.filter(u).each(o), this
    }, e(function (e) {
        var a = e(t);
        a.on("submit", "form", r), a.on("focus", u, n), a.on("blur", u, o), e(u).placeholder()
    }))
}(jQuery, document, window.debug), jQuery.easing.jswing = jQuery.easing.swing, jQuery.extend(jQuery.easing, {
    def: "easeOutQuad",
    swing: function (e, t, a, r, i) {
        return jQuery.easing[jQuery.easing.def](e, t, a, r, i)
    },
    easeInQuad: function (e, t, a, r, i) {
        return r * (t /= i) * t + a
    },
    easeOutQuad: function (e, t, a, r, i) {
        return -r * (t /= i) * (t - 2) + a
    },
    easeInOutQuad: function (e, t, a, r, i) {
        return (t /= i / 2) < 1 ? r / 2 * t * t + a : -r / 2 * (--t * (t - 2) - 1) + a
    },
    easeInCubic: function (e, t, a, r, i) {
        return r * (t /= i) * t * t + a
    },
    easeOutCubic: function (e, t, a, r, i) {
        return r * ((t = t / i - 1) * t * t + 1) + a
    },
    easeInOutCubic: function (e, t, a, r, i) {
        return (t /= i / 2) < 1 ? r / 2 * t * t * t + a : r / 2 * ((t -= 2) * t * t + 2) + a
    },
    easeInQuart: function (e, t, a, r, i) {
        return r * (t /= i) * t * t * t + a
    },
    easeOutQuart: function (e, t, a, r, i) {
        return -r * ((t = t / i - 1) * t * t * t - 1) + a
    },
    easeInOutQuart: function (e, t, a, r, i) {
        return (t /= i / 2) < 1 ? r / 2 * t * t * t * t + a : -r / 2 * ((t -= 2) * t * t * t - 2) + a
    },
    easeInQuint: function (e, t, a, r, i) {
        return r * (t /= i) * t * t * t * t + a
    },
    easeOutQuint: function (e, t, a, r, i) {
        return r * ((t = t / i - 1) * t * t * t * t + 1) + a
    },
    easeInOutQuint: function (e, t, a, r, i) {
        return (t /= i / 2) < 1 ? r / 2 * t * t * t * t * t + a : r / 2 * ((t -= 2) * t * t * t * t + 2) + a
    },
    easeInSine: function (e, t, a, r, i) {
        return -r * Math.cos(t / i * (Math.PI / 2)) + r + a
    },
    easeOutSine: function (e, t, a, r, i) {
        return r * Math.sin(t / i * (Math.PI / 2)) + a
    },
    easeInOutSine: function (e, t, a, r, i) {
        return -r / 2 * (Math.cos(Math.PI * t / i) - 1) + a
    },
    easeInExpo: function (e, t, a, r, i) {
        return 0 == t ? a : r * Math.pow(2, 10 * (t / i - 1)) + a
    },
    easeOutExpo: function (e, t, a, r, i) {
        return t == i ? a + r : r * (-Math.pow(2, -10 * t / i) + 1) + a
    },
    easeInOutExpo: function (e, t, a, r, i) {
        return 0 == t ? a : t == i ? a + r : (t /= i / 2) < 1 ? r / 2 * Math.pow(2, 10 * (t - 1)) + a : r / 2 * (-Math.pow(2, -10 * --t) + 2) + a
    },
    easeInCirc: function (e, t, a, r, i) {
        return -r * (Math.sqrt(1 - (t /= i) * t) - 1) + a
    },
    easeOutCirc: function (e, t, a, r, i) {
        return r * Math.sqrt(1 - (t = t / i - 1) * t) + a
    },
    easeInOutCirc: function (e, t, a, r, i) {
        return (t /= i / 2) < 1 ? -r / 2 * (Math.sqrt(1 - t * t) - 1) + a : r / 2 * (Math.sqrt(1 - (t -= 2) * t) + 1) + a
    },
    easeInElastic: function (e, t, a, r, i) {
        var n = 1.70158,
            o = 0,
            l = r;
        if (0 == t) return a;
        if (1 == (t /= i)) return a + r;
        if (o || (o = .3 * i), l < Math.abs(r)) {
            l = r;
            var n = o / 4
        } else var n = o / (2 * Math.PI) * Math.asin(r / l);
        return -(l * Math.pow(2, 10 * (t -= 1)) * Math.sin(2 * (t * i - n) * Math.PI / o)) + a
    },
    easeOutElastic: function (e, t, a, r, i) {
        var n = 1.70158,
            o = 0,
            l = r;
        if (0 == t) return a;
        if (1 == (t /= i)) return a + r;
        if (o || (o = .3 * i), l < Math.abs(r)) {
            l = r;
            var n = o / 4
        } else var n = o / (2 * Math.PI) * Math.asin(r / l);
        return l * Math.pow(2, -10 * t) * Math.sin(2 * (t * i - n) * Math.PI / o) + r + a
    },
    easeInOutElastic: function (e, t, a, r, i) {
        var n = 1.70158,
            o = 0,
            l = r;
        if (0 == t) return a;
        if (2 == (t /= i / 2)) return a + r;
        if (o || (o = .3 * i * 1.5), l < Math.abs(r)) {
            l = r;
            var n = o / 4
        } else var n = o / (2 * Math.PI) * Math.asin(r / l);
        return 1 > t ? -.5 * l * Math.pow(2, 10 * (t -= 1)) * Math.sin(2 * (t * i - n) * Math.PI / o) + a : l * Math.pow(2, -10 * (t -= 1)) * Math.sin(2 * (t * i - n) * Math.PI / o) * .5 + r + a
    },
    easeInBack: function (e, t, a, r, i, n) {
        return void 0 == n && (n = 1.70158), r * (t /= i) * t * ((n + 1) * t - n) + a
    },
    easeOutBack: function (e, t, a, r, i, n) {
        return void 0 == n && (n = 1.70158), r * ((t = t / i - 1) * t * ((n + 1) * t + n) + 1) + a
    },
    easeInOutBack: function (e, t, a, r, i, n) {
        return void 0 == n && (n = 1.70158), (t /= i / 2) < 1 ? r / 2 * t * t * (((n *= 1.525) + 1) * t - n) + a : r / 2 * ((t -= 2) * t * (((n *= 1.525) + 1) * t + n) + 2) + a
    },
    easeInBounce: function (e, t, a, r, i) {
        return r - jQuery.easing.easeOutBounce(e, i - t, 0, r, i) + a
    },
    easeOutBounce: function (e, t, a, r, i) {
        return (t /= i) < 1 / 2.75 ? 7.5625 * r * t * t + a : 2 / 2.75 > t ? r * (7.5625 * (t -= 1.5 / 2.75) * t + .75) + a : 2.5 / 2.75 > t ? r * (7.5625 * (t -= 2.25 / 2.75) * t + .9375) + a : r * (7.5625 * (t -= 2.625 / 2.75) * t + .984375) + a
    },
    easeInOutBounce: function (e, t, a, r, i) {
        return i / 2 > t ? .5 * jQuery.easing.easeInBounce(e, 2 * t, 0, r, i) + a : .5 * jQuery.easing.easeOutBounce(e, 2 * t - i, 0, r, i) + .5 * r + a
    }
}),
    function (e) {
        e.fn.extend({
            slimScroll: function (a) {
                var r = e.extend({
                    width: "auto",
                    height: "250px",
                    size: "7px",
                    color: "#000",
                    position: "right",
                    distance: "1px",
                    start: "top",
                    opacity: .4,
                    alwaysVisible: !1,
                    disableFadeOut: !1,
                    railVisible: !1,
                    railColor: "#333",
                    railOpacity: .2,
                    railDraggable: !0,
                    railClass: "slimScrollRail",
                    barClass: "slimScrollBar",
                    wrapperClass: "slimScrollDiv",
                    allowPageScroll: !1,
                    wheelStep: 20,
                    touchScrollStep: 200,
                    borderRadius: "7px",
                    railBorderRadius: "7px"
                }, a);
                return this.each(function () {
                    function i(t) {
                        if (c) {
                            t = t || window.event;
                            var a = 0;
                            t.wheelDelta && (a = -t.wheelDelta / 120), t.detail && (a = t.detail / 3), e(t.target || t.srcTarget || t.srcElement).closest("." + r.wrapperClass).is(j.parent()) && n(a, !0), t.preventDefault && !v && t.preventDefault(), v || (t.returnValue = !1)
                        }
                    }

                    function n(e, t, a) {
                        v = !1;
                        var i = e,
                            n = j.outerHeight() - w.outerHeight();
                        t && (i = parseInt(w.css("top")) + e * parseInt(r.wheelStep) / 100 * w.outerHeight(), i = Math.min(Math.max(i, 0), n), i = e > 0 ? Math.ceil(i) : Math.floor(i), w.css({
                            top: i + "px"
                        })), y = parseInt(w.css("top")) / (j.outerHeight() - w.outerHeight()), i = y * (j[0].scrollHeight - j.outerHeight()), a && (i = e, e = i / j[0].scrollHeight * j.outerHeight(), e = Math.min(Math.max(e, 0), n), w.css({
                            top: e + "px"
                        })), j.scrollTop(i), j.trigger("slimscrolling", ~~i), s(), u()
                    }

                    function o() {
                        window.addEventListener ? (this.addEventListener("DOMMouseScroll", i, !1), this.addEventListener("mousewheel", i, !1)) : document.attachEvent("onmousewheel", i)
                    }

                    function l() {
                        m = Math.max(j.outerHeight() / j[0].scrollHeight * j.outerHeight(), 30), w.css({
                            height: m + "px"
                        });
                        var e = m == j.outerHeight() ? "none" : "block";
                        w.css({
                            display: e
                        })
                    }

                    function s() {
                        l(), clearTimeout(h), y == ~~y ? (v = r.allowPageScroll, g != y && j.trigger("slimscroll", 0 == ~~y ? "top" : "bottom")) : v = !1, g = y, m >= j.outerHeight() ? v = !0 : (w.stop(!0, !0).fadeIn("fast"), r.railVisible && b.stop(!0, !0).fadeIn("fast"))
                    }

                    function u() {
                        r.alwaysVisible || (h = setTimeout(function () {
                            r.disableFadeOut && c || d || p || (w.fadeOut("slow"), b.fadeOut("slow"))
                        }, 1e3))
                    }
                    var c, d, p, h, f, m, y, g, v = !1,
                        j = e(this);
                    if (j.parent().hasClass(r.wrapperClass)) {
                        var Q = j.scrollTop(),
                            w = j.parent().find("." + r.barClass),
                            b = j.parent().find("." + r.railClass);
                        if (l(), e.isPlainObject(a)) {
                            if ("height" in a && "auto" == a.height) {
                                j.parent().css("height", "auto"), j.css("height", "auto");
                                var _ = j.parent().parent().height();
                                j.parent().css("height", _), j.css("height", _)
                            }
                            if ("scrollTo" in a) Q = parseInt(r.scrollTo);
                            else if ("scrollBy" in a) Q += parseInt(r.scrollBy);
                            else if ("destroy" in a) return w.remove(), b.remove(), void j.unwrap();
                            n(Q, !1, !0)
                        }
                    } else if (!(e.isPlainObject(a) && "destroy" in a)) {
                        r.height = "auto" == r.height ? j.parent().height() : r.height, Q = e("<div></div>").addClass(r.wrapperClass).css({
                            position: "relative",
                            overflow: "hidden",
                            width: r.width,
                            height: r.height
                        }), j.css({
                            overflow: "hidden",
                            width: r.width,
                            height: r.height
                        });
                        var b = e("<div></div>").addClass(r.railClass).css({
                            width: r.size,
                            height: "100%",
                            position: "absolute",
                            top: 0,
                            display: r.alwaysVisible && r.railVisible ? "block" : "none",
                            "border-radius": r.railBorderRadius,
                            background: r.railColor,
                            opacity: r.railOpacity,
                            zIndex: 90
                        }),
                            w = e("<div></div>").addClass(r.barClass).css({
                                background: r.color,
                                width: r.size,
                                position: "absolute",
                                top: 0,
                                opacity: r.opacity,
                                display: r.alwaysVisible ? "block" : "none",
                                "border-radius": r.borderRadius,
                                BorderRadius: r.borderRadius,
                                MozBorderRadius: r.borderRadius,
                                WebkitBorderRadius: r.borderRadius,
                                zIndex: 99
                            }),
                            _ = "right" == r.position ? {
                                right: r.distance
                            } : {
                                left: r.distance
                            };
                        b.css(_), w.css(_), j.wrap(Q), j.parent().append(w), j.parent().append(b), r.railDraggable && w.bind("mousedown", function (a) {
                            var r = e(document);
                            return p = !0, t = parseFloat(w.css("top")), pageY = a.pageY, r.bind("mousemove.slimscroll", function (e) {
                                currTop = t + e.pageY - pageY, w.css("top", currTop), n(0, w.position().top, !1)
                            }), r.bind("mouseup.slimscroll", function () {
                                p = !1, u(), r.unbind(".slimscroll")
                            }), !1
                        }).bind("selectstart.slimscroll", function (e) {
                            return e.stopPropagation(), e.preventDefault(), !1
                        }), b.hover(function () {
                            s()
                        }, function () {
                            u()
                        }), w.hover(function () {
                            d = !0
                        }, function () {
                            d = !1
                        }), j.hover(function () {
                            c = !0, s(), u()
                        }, function () {
                            c = !1, u()
                        }), j.bind("touchstart", function (e) {
                            e.originalEvent.touches.length && (f = e.originalEvent.touches[0].pageY)
                        }), j.bind("touchmove", function (e) {
                            v || e.originalEvent.preventDefault(), e.originalEvent.touches.length && (n((f - e.originalEvent.touches[0].pageY) / r.touchScrollStep, !0), f = e.originalEvent.touches[0].pageY)
                        }), l(), "bottom" === r.start ? (w.css({
                            top: j.outerHeight() - w.outerHeight()
                        }), n(0, !0)) : "top" !== r.start && (n(e(r.start).position().top, null, !0), r.alwaysVisible || w.hide()), o()
                    }
                }), this
            }
        }), e.fn.extend({
            slimscroll: e.fn.slimScroll
        })
    }(jQuery);