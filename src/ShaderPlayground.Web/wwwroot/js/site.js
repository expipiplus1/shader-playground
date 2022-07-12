// No CDN
// Just add ansi_up, from https://github.com/drudru/ansi_up/blob/master/ansi_up.js

/*  ansi_up.js
 *  author : Dru Nelson
 *  license : MIT
 *  http://github.com/drudru/ansi_up
 */
(function (root, factory) {
    if (typeof define === 'function' && define.amd) {
        // AMD. Register as an anonymous module.
        define(['exports'], factory);
    } else if (typeof exports === 'object' && typeof exports.nodeName !== 'string') {
        // CommonJS
        factory(exports);
    } else {
        // Browser globals
        var exp = {};
        factory(exp);
        root.AnsiUp = exp.default;
    }
}(this, function (exports) {
"use strict";
var __makeTemplateObject = (this && this.__makeTemplateObject) || function (cooked, raw) {
    if (Object.defineProperty) { Object.defineProperty(cooked, "raw", { value: raw }); } else { cooked.raw = raw; }
    return cooked;
};
var PacketKind;
(function (PacketKind) {
    PacketKind[PacketKind["EOS"] = 0] = "EOS";
    PacketKind[PacketKind["Text"] = 1] = "Text";
    PacketKind[PacketKind["Incomplete"] = 2] = "Incomplete";
    PacketKind[PacketKind["ESC"] = 3] = "ESC";
    PacketKind[PacketKind["Unknown"] = 4] = "Unknown";
    PacketKind[PacketKind["SGR"] = 5] = "SGR";
    PacketKind[PacketKind["OSCURL"] = 6] = "OSCURL";
})(PacketKind || (PacketKind = {}));
var AnsiUp = (function () {
    function AnsiUp() {
        this.VERSION = "5.1.0";
        this.setup_palettes();
        this._use_classes = false;
        this.bold = false;
        this.italic = false;
        this.underline = false;
        this.fg = this.bg = null;
        this._buffer = '';
        this._url_whitelist = { 'http': 1, 'https': 1 };
    }
    Object.defineProperty(AnsiUp.prototype, "use_classes", {
        get: function () {
            return this._use_classes;
        },
        set: function (arg) {
            this._use_classes = arg;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(AnsiUp.prototype, "url_whitelist", {
        get: function () {
            return this._url_whitelist;
        },
        set: function (arg) {
            this._url_whitelist = arg;
        },
        enumerable: false,
        configurable: true
    });
    AnsiUp.prototype.setup_palettes = function () {
        var _this = this;
        this.ansi_colors =
            [
                [
                    { rgb: [0, 0, 0], class_name: "ansi-black" },
                    { rgb: [187, 0, 0], class_name: "ansi-red" },
                    { rgb: [0, 187, 0], class_name: "ansi-green" },
                    { rgb: [187, 187, 0], class_name: "ansi-yellow" },
                    { rgb: [0, 0, 187], class_name: "ansi-blue" },
                    { rgb: [187, 0, 187], class_name: "ansi-magenta" },
                    { rgb: [0, 187, 187], class_name: "ansi-cyan" },
                    { rgb: [255, 255, 255], class_name: "ansi-white" }
                ],
                [
                    { rgb: [85, 85, 85], class_name: "ansi-bright-black" },
                    { rgb: [255, 85, 85], class_name: "ansi-bright-red" },
                    { rgb: [0, 255, 0], class_name: "ansi-bright-green" },
                    { rgb: [255, 255, 85], class_name: "ansi-bright-yellow" },
                    { rgb: [85, 85, 255], class_name: "ansi-bright-blue" },
                    { rgb: [255, 85, 255], class_name: "ansi-bright-magenta" },
                    { rgb: [85, 255, 255], class_name: "ansi-bright-cyan" },
                    { rgb: [255, 255, 255], class_name: "ansi-bright-white" }
                ]
            ];
        this.palette_256 = [];
        this.ansi_colors.forEach(function (palette) {
            palette.forEach(function (rec) {
                _this.palette_256.push(rec);
            });
        });
        var levels = [0, 95, 135, 175, 215, 255];
        for (var r = 0; r < 6; ++r) {
            for (var g = 0; g < 6; ++g) {
                for (var b = 0; b < 6; ++b) {
                    var col = { rgb: [levels[r], levels[g], levels[b]], class_name: 'truecolor' };
                    this.palette_256.push(col);
                }
            }
        }
        var grey_level = 8;
        for (var i = 0; i < 24; ++i, grey_level += 10) {
            var gry = { rgb: [grey_level, grey_level, grey_level], class_name: 'truecolor' };
            this.palette_256.push(gry);
        }
    };
    AnsiUp.prototype.escape_txt_for_html = function (txt) {
        return txt.replace(/[&<>"']/gm, function (str) {
            if (str === "&")
                return "&amp;";
            if (str === "<")
                return "&lt;";
            if (str === ">")
                return "&gt;";
            if (str === "\"")
                return "&quot;";
            if (str === "'")
                return "&#x27;";
        });
    };
    AnsiUp.prototype.append_buffer = function (txt) {
        var str = this._buffer + txt;
        this._buffer = str;
    };
    AnsiUp.prototype.get_next_packet = function () {
        var pkt = {
            kind: PacketKind.EOS,
            text: '',
            url: ''
        };
        var len = this._buffer.length;
        if (len == 0)
            return pkt;
        var pos = this._buffer.indexOf("\x1B");
        if (pos == -1) {
            pkt.kind = PacketKind.Text;
            pkt.text = this._buffer;
            this._buffer = '';
            return pkt;
        }
        if (pos > 0) {
            pkt.kind = PacketKind.Text;
            pkt.text = this._buffer.slice(0, pos);
            this._buffer = this._buffer.slice(pos);
            return pkt;
        }
        if (pos == 0) {
            if (len == 1) {
                pkt.kind = PacketKind.Incomplete;
                return pkt;
            }
            var next_char = this._buffer.charAt(1);
            if ((next_char != '[') && (next_char != ']')) {
                pkt.kind = PacketKind.ESC;
                pkt.text = this._buffer.slice(0, 1);
                this._buffer = this._buffer.slice(1);
                return pkt;
            }
            if (next_char == '[') {
                if (!this._csi_regex) {
                    this._csi_regex = rgx(__makeTemplateObject(["\n                        ^                           # beginning of line\n                                                    #\n                                                    # First attempt\n                        (?:                         # legal sequence\n                          \u001B[                      # CSI\n                          ([<-?]?)              # private-mode char\n                          ([d;]*)                    # any digits or semicolons\n                          ([ -/]?               # an intermediate modifier\n                          [@-~])                # the command\n                        )\n                        |                           # alternate (second attempt)\n                        (?:                         # illegal sequence\n                          \u001B[                      # CSI\n                          [ -~]*                # anything legal\n                          ([\0-\u001F:])              # anything illegal\n                        )\n                    "], ["\n                        ^                           # beginning of line\n                                                    #\n                                                    # First attempt\n                        (?:                         # legal sequence\n                          \\x1b\\[                      # CSI\n                          ([\\x3c-\\x3f]?)              # private-mode char\n                          ([\\d;]*)                    # any digits or semicolons\n                          ([\\x20-\\x2f]?               # an intermediate modifier\n                          [\\x40-\\x7e])                # the command\n                        )\n                        |                           # alternate (second attempt)\n                        (?:                         # illegal sequence\n                          \\x1b\\[                      # CSI\n                          [\\x20-\\x7e]*                # anything legal\n                          ([\\x00-\\x1f:])              # anything illegal\n                        )\n                    "]));
                }
                var match = this._buffer.match(this._csi_regex);
                if (match === null) {
                    pkt.kind = PacketKind.Incomplete;
                    return pkt;
                }
                if (match[4]) {
                    pkt.kind = PacketKind.ESC;
                    pkt.text = this._buffer.slice(0, 1);
                    this._buffer = this._buffer.slice(1);
                    return pkt;
                }
                if ((match[1] != '') || (match[3] != 'm'))
                    pkt.kind = PacketKind.Unknown;
                else
                    pkt.kind = PacketKind.SGR;
                pkt.text = match[2];
                var rpos = match[0].length;
                this._buffer = this._buffer.slice(rpos);
                return pkt;
            }
            if (next_char == ']') {
                if (len < 4) {
                    pkt.kind = PacketKind.Incomplete;
                    return pkt;
                }
                if ((this._buffer.charAt(2) != '8')
                    || (this._buffer.charAt(3) != ';')) {
                    pkt.kind = PacketKind.ESC;
                    pkt.text = this._buffer.slice(0, 1);
                    this._buffer = this._buffer.slice(1);
                    return pkt;
                }
                if (!this._osc_st) {
                    this._osc_st = rgxG(__makeTemplateObject(["\n                        (?:                         # legal sequence\n                          (\u001B\\)                    # ESC                           |                           # alternate\n                          (\u0007)                      # BEL (what xterm did)\n                        )\n                        |                           # alternate (second attempt)\n                        (                           # illegal sequence\n                          [\0-\u0006]                 # anything illegal\n                          |                           # alternate\n                          [\b-\u001A]                 # anything illegal\n                          |                           # alternate\n                          [\u001C-\u001F]                 # anything illegal\n                        )\n                    "], ["\n                        (?:                         # legal sequence\n                          (\\x1b\\\\)                    # ESC \\\n                          |                           # alternate\n                          (\\x07)                      # BEL (what xterm did)\n                        )\n                        |                           # alternate (second attempt)\n                        (                           # illegal sequence\n                          [\\x00-\\x06]                 # anything illegal\n                          |                           # alternate\n                          [\\x08-\\x1a]                 # anything illegal\n                          |                           # alternate\n                          [\\x1c-\\x1f]                 # anything illegal\n                        )\n                    "]));
                }
                this._osc_st.lastIndex = 0;
                {
                    var match_1 = this._osc_st.exec(this._buffer);
                    if (match_1 === null) {
                        pkt.kind = PacketKind.Incomplete;
                        return pkt;
                    }
                    if (match_1[3]) {
                        pkt.kind = PacketKind.ESC;
                        pkt.text = this._buffer.slice(0, 1);
                        this._buffer = this._buffer.slice(1);
                        return pkt;
                    }
                }
                {
                    var match_2 = this._osc_st.exec(this._buffer);
                    if (match_2 === null) {
                        pkt.kind = PacketKind.Incomplete;
                        return pkt;
                    }
                    if (match_2[3]) {
                        pkt.kind = PacketKind.ESC;
                        pkt.text = this._buffer.slice(0, 1);
                        this._buffer = this._buffer.slice(1);
                        return pkt;
                    }
                }
                if (!this._osc_regex) {
                    this._osc_regex = rgx(__makeTemplateObject(["\n                        ^                           # beginning of line\n                                                    #\n                        \u001B]8;                    # OSC Hyperlink\n                        [ -:<-~]*       # params (excluding ;)\n                        ;                           # end of params\n                        ([!-~]{0,512})        # URL capture\n                        (?:                         # ST\n                          (?:\u001B\\)                  # ESC                           |                           # alternate\n                          (?:\u0007)                    # BEL (what xterm did)\n                        )\n                        ([ -~]+)              # TEXT capture\n                        \u001B]8;;                   # OSC Hyperlink End\n                        (?:                         # ST\n                          (?:\u001B\\)                  # ESC                           |                           # alternate\n                          (?:\u0007)                    # BEL (what xterm did)\n                        )\n                    "], ["\n                        ^                           # beginning of line\n                                                    #\n                        \\x1b\\]8;                    # OSC Hyperlink\n                        [\\x20-\\x3a\\x3c-\\x7e]*       # params (excluding ;)\n                        ;                           # end of params\n                        ([\\x21-\\x7e]{0,512})        # URL capture\n                        (?:                         # ST\n                          (?:\\x1b\\\\)                  # ESC \\\n                          |                           # alternate\n                          (?:\\x07)                    # BEL (what xterm did)\n                        )\n                        ([\\x20-\\x7e]+)              # TEXT capture\n                        \\x1b\\]8;;                   # OSC Hyperlink End\n                        (?:                         # ST\n                          (?:\\x1b\\\\)                  # ESC \\\n                          |                           # alternate\n                          (?:\\x07)                    # BEL (what xterm did)\n                        )\n                    "]));
                }
                var match = this._buffer.match(this._osc_regex);
                if (match === null) {
                    pkt.kind = PacketKind.ESC;
                    pkt.text = this._buffer.slice(0, 1);
                    this._buffer = this._buffer.slice(1);
                    return pkt;
                }
                pkt.kind = PacketKind.OSCURL;
                pkt.url = match[1];
                pkt.text = match[2];
                var rpos = match[0].length;
                this._buffer = this._buffer.slice(rpos);
                return pkt;
            }
        }
    };
    AnsiUp.prototype.ansi_to_html = function (txt) {
        this.append_buffer(txt);
        var blocks = [];
        while (true) {
            var packet = this.get_next_packet();
            if ((packet.kind == PacketKind.EOS)
                || (packet.kind == PacketKind.Incomplete))
                break;
            if ((packet.kind == PacketKind.ESC)
                || (packet.kind == PacketKind.Unknown))
                continue;
            if (packet.kind == PacketKind.Text)
                blocks.push(this.transform_to_html(this.with_state(packet)));
            else if (packet.kind == PacketKind.SGR)
                this.process_ansi(packet);
            else if (packet.kind == PacketKind.OSCURL)
                blocks.push(this.process_hyperlink(packet));
        }
        return blocks.join("");
    };
    AnsiUp.prototype.with_state = function (pkt) {
        return { bold: this.bold, italic: this.italic, underline: this.underline, fg: this.fg, bg: this.bg, text: pkt.text };
    };
    AnsiUp.prototype.process_ansi = function (pkt) {
        var sgr_cmds = pkt.text.split(';');
        while (sgr_cmds.length > 0) {
            var sgr_cmd_str = sgr_cmds.shift();
            var num = parseInt(sgr_cmd_str, 10);
            if (isNaN(num) || num === 0) {
                this.fg = this.bg = null;
                this.bold = false;
                this.italic = false;
                this.underline = false;
            }
            else if (num === 1) {
                this.bold = true;
            }
            else if (num === 3) {
                this.italic = true;
            }
            else if (num === 4) {
                this.underline = true;
            }
            else if (num === 22) {
                this.bold = false;
            }
            else if (num === 23) {
                this.italic = false;
            }
            else if (num === 24) {
                this.underline = false;
            }
            else if (num === 39) {
                this.fg = null;
            }
            else if (num === 49) {
                this.bg = null;
            }
            else if ((num >= 30) && (num < 38)) {
                this.fg = this.ansi_colors[0][(num - 30)];
            }
            else if ((num >= 40) && (num < 48)) {
                this.bg = this.ansi_colors[0][(num - 40)];
            }
            else if ((num >= 90) && (num < 98)) {
                this.fg = this.ansi_colors[1][(num - 90)];
            }
            else if ((num >= 100) && (num < 108)) {
                this.bg = this.ansi_colors[1][(num - 100)];
            }
            else if (num === 38 || num === 48) {
                if (sgr_cmds.length > 0) {
                    var is_foreground = (num === 38);
                    var mode_cmd = sgr_cmds.shift();
                    if (mode_cmd === '5' && sgr_cmds.length > 0) {
                        var palette_index = parseInt(sgr_cmds.shift(), 10);
                        if (palette_index >= 0 && palette_index <= 255) {
                            if (is_foreground)
                                this.fg = this.palette_256[palette_index];
                            else
                                this.bg = this.palette_256[palette_index];
                        }
                    }
                    if (mode_cmd === '2' && sgr_cmds.length > 2) {
                        var r = parseInt(sgr_cmds.shift(), 10);
                        var g = parseInt(sgr_cmds.shift(), 10);
                        var b = parseInt(sgr_cmds.shift(), 10);
                        if ((r >= 0 && r <= 255) && (g >= 0 && g <= 255) && (b >= 0 && b <= 255)) {
                            var c = { rgb: [r, g, b], class_name: 'truecolor' };
                            if (is_foreground)
                                this.fg = c;
                            else
                                this.bg = c;
                        }
                    }
                }
            }
        }
    };
    AnsiUp.prototype.transform_to_html = function (fragment) {
        var txt = fragment.text;
        if (txt.length === 0)
            return txt;
        txt = this.escape_txt_for_html(txt);
        if (!fragment.bold && !fragment.italic && !fragment.underline && fragment.fg === null && fragment.bg === null)
            return txt;
        var styles = [];
        var classes = [];
        var fg = fragment.fg;
        var bg = fragment.bg;
        if (fragment.bold)
            styles.push('font-weight:bold');
        if (fragment.italic)
            styles.push('font-style:italic');
        if (fragment.underline)
            styles.push('text-decoration:underline');
        if (!this._use_classes) {
            if (fg)
                styles.push("color:rgb(" + fg.rgb.join(',') + ")");
            if (bg)
                styles.push("background-color:rgb(" + bg.rgb + ")");
        }
        else {
            if (fg) {
                if (fg.class_name !== 'truecolor') {
                    classes.push(fg.class_name + "-fg");
                }
                else {
                    styles.push("color:rgb(" + fg.rgb.join(',') + ")");
                }
            }
            if (bg) {
                if (bg.class_name !== 'truecolor') {
                    classes.push(bg.class_name + "-bg");
                }
                else {
                    styles.push("background-color:rgb(" + bg.rgb.join(',') + ")");
                }
            }
        }
        var class_string = '';
        var style_string = '';
        if (classes.length)
            class_string = " class=\"" + classes.join(' ') + "\"";
        if (styles.length)
            style_string = " style=\"" + styles.join(';') + "\"";
        return "<span" + style_string + class_string + ">" + txt + "</span>";
    };
    ;
    AnsiUp.prototype.process_hyperlink = function (pkt) {
        var parts = pkt.url.split(':');
        if (parts.length < 1)
            return '';
        if (!this._url_whitelist[parts[0]])
            return '';
        var result = "<a href=\"" + this.escape_txt_for_html(pkt.url) + "\">" + this.escape_txt_for_html(pkt.text) + "</a>";
        return result;
    };
    return AnsiUp;
}());
function rgx(tmplObj) {
    var subst = [];
    for (var _i = 1; _i < arguments.length; _i++) {
        subst[_i - 1] = arguments[_i];
    }
    var regexText = tmplObj.raw[0];
    var wsrgx = /^\s+|\s+\n|\s*#[\s\S]*?\n|\n/gm;
    var txt2 = regexText.replace(wsrgx, '');
    return new RegExp(txt2);
}
function rgxG(tmplObj) {
    var subst = [];
    for (var _i = 1; _i < arguments.length; _i++) {
        subst[_i - 1] = arguments[_i];
    }
    var regexText = tmplObj.raw[0];
    var wsrgx = /^\s+|\s+\n|\s*#[\s\S]*?\n|\n/gm;
    var txt2 = regexText.replace(wsrgx, '');
    return new RegExp(txt2, 'g');
}
//# sourceMappingURL=ansi_up.js.map
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.default = AnsiUp;
}));

$(function () {
    /**
     * @typedef {('TextBox'|'ComboBox'|'CheckBox')} ShaderCompilerParameterType
     */

    /**
     * @typedef ShaderCompilerParameter
     * @property {string} name
     * @property {string} displayName
     * @property {ShaderCompilerParameterType} parameterType
     * @property {string[]} options
     * @property {string} defaultValue
     * @property {string} description
     * @property {ParameterFilter} filter
    */

    /**
     * @typedef ParameterFilter
     * @property {string} name
     * @property {string[]} values
     */

    /**
     * @typedef ShaderCompiler
     * @property {string} name
     * @property {string} displayName
     * @property {string} description
     * @property {string[]} inputLanguages
     * @property {ShaderCompilerParameter[]} parameters
     * @property {string[]} outputLanguages
     */

    /**
     * @typedef ShaderLanguage
     * @property {string} name -
     * @property {string} defaultCode -
     */

    var counter = 0;
    function uniqueId() {
        return 'myid-' + counter++;
    }

    class ParameterEditor {
        /**
         * @param {ShaderCompilerParameter} parameter -
         * @returns {ParameterEditor} -
         */
        static create(parameter) {
            switch (parameter.parameterType) {
                case "TextBox":
                    return new TextBoxParameterEditor(parameter);

                case "ComboBox":
                    return new ComboBoxParameterEditor(parameter);

                case "CheckBox":
                    return new CheckBoxParameterEditor(parameter);

                default:
                    throw `Unexpected parameter type: ${parameter.parameterType}`;
            }
        }

        /**
         * @param {ShaderCompilerParameter} parameter
         */
        constructor(parameter, templateId, initializeElementCallback) {
            this.parameter = parameter;

            /** @type {HTMLTemplateElement} */
            var template = document.getElementById(templateId);

            var inputElementId = `parameter-editor-${uniqueId()}`;

            this.element = document.importNode(template.content, true);

            let labelElement = this.element.querySelector("label");
            labelElement.htmlFor = inputElementId;
            labelElement.textContent = parameter.displayName;

            this.inputElement = this.element.querySelector("[data-isinputelement]");
            this.inputElement.id = inputElementId;
            this.inputElement.title = parameter.displayName;

            this.descriptionElement = this.element.querySelector("small");
            this.descriptionElement.innerText = parameter.description;

            initializeElementCallback(this.inputElement);
        }

        get value() { return ""; /* Should be overridden */ }
        set value(x) { /* Should be overridden */ }

        get isLanguageOutput() {
            return this.parameter.name === "OutputLanguage";
        }

        get isVersion() {
            return this.parameter.name === "Version";
        }
    }

    class TextBoxParameterEditor extends ParameterEditor {
        /** @param {ShaderCompilerParameter} parameter */
        constructor(parameter) {
            super(
                parameter,
                "textbox-parameter-template",
                x => x.defaultValue = parameter.defaultValue);
        }

        get value() {
            return this.inputElement.value;
        }

        set value(x) {
            this.inputElement.value = x;
        }
    }

    class ComboBoxParameterEditor extends ParameterEditor {
        /** @param {ShaderCompilerParameter} parameter */
        constructor(parameter) {
            super(
                parameter,
                "combobox-parameter-template",
                x => {
                    for (let option of parameter.options) {
                        let isSelected = (option === parameter.defaultValue);
                        x.options.add(new Option(option, option, isSelected, isSelected));
                    }
                });
        }
        
        get value() {
            return this.inputElement.value;
        }

        set value(x) {
            this.inputElement.value = x;
        }
    }

    class CheckBoxParameterEditor extends ParameterEditor {
        /** @param {ShaderCompilerParameter} parameter */
        constructor(parameter) {
            super(
                parameter,
                "checkbox-parameter-template",
                x => x.checked = (parameter.defaultValue === "true"));
        }

        get value() {
            return this.inputElement.checked ? "true" : "false";
        }

        set value(x) {
            this.inputElement.checked = (x === "true");
        }
    }

    /** @type {ShaderLanguage[]} */
    var shaderLanguages = window.ShaderLanguages;

    /** @type {ShaderCompiler[]} */
    var shaderCompilers = window.ShaderCompilers;

    /** @type {CodeMirror.Editor} */
    var outputEditor = null;

    const codeMirrorTheme = "default";

    var codeEditor = CodeMirror.fromTextArea(document.getElementById('code'), {
        mode: "x-shader/hlsl",
        theme: codeMirrorTheme,
        lineNumbers: true,
        matchBrackets: true,
        styleActiveLine: true,
        indentUnit: 4
    });

    /** @type {HTMLSelectElement} */
    var languageSelect = document.getElementById('language');

    for (var language of shaderLanguages) {
        languageSelect.options.add(new Option(language.name, language.name));
    }

    /** @type {HTMLSelectElement} */
    var outputStepsSelect = document.getElementById('output-steps');
    var outputTabsSelect = document.getElementById('output-tabs');
    var outputContainerDiv = document.getElementById('output-container');

    function getSelectedLanguage() {
        return shaderLanguages.find(x => x.name === languageSelect.selectedOptions[0].value);
    }

    function getCodeMirrorMode(language) {
        switch (language) {
            case "GLSL":
                return "x-shader/glsl";

            case "HLSL":
            case "Slang":
                return "x-shader/hlsl";

            case "DXBC":
                return "text/x-dxbc";

            case "DXIL":
            case "Metal IR":
                return "text/x-llvm-ir";

            case "OpenCL C":
            case "ISPC":
            case "C++":
            case "Metal":
                return "text/x-c";

            case "SPIR-V":
            case "SPIR-V ASM":
                return "text/x-spirv";

            case "Rust":
                return "text/rust";

            default:
                return "text/plain";
        }
    }

    function createJsonRequestObject() {
        var jsonObject = {
            language: getSelectedLanguage().name,
            code: codeEditor.getValue()
        };

        var compilationSteps = [];

        for (let compilerEditor of compilerEditors.compilerEditors) {
            var jsonArguments = {};
            for (var parameterEditor of compilerEditor.parameterEditors) {
                jsonArguments[parameterEditor.parameter.name] = parameterEditor.value;
            }

            compilationSteps.push({
                compiler: compilerEditor.selectedCompiler.name,
                arguments: jsonArguments
            });
        }

        jsonObject.compilationSteps = compilationSteps;

        return jsonObject;
    }

    function compileCode() {
        var jsonObject = createJsonRequestObject();

        let selectedLanguage = getSelectedLanguage();

        gtag('event', 'CompileShader', {
            "languageName": selectedLanguage.name,
            "compilerNames": compilerEditors.compilerEditors.map(c => c.selectedCompiler.displayName).join(", "),
            "isDefaultCode": (selectedLanguage.defaultCode.replace(/\r\n/g, "\n") == jsonObject.code) ? "Default" : "Not default",
            "numCompilers": compilerEditors.compilerEditors.length,
            "shaderSize": jsonObject.code.length
        });

        $("#output-container").addClass("loading");
        $("#output-loading").show();

        function finishLoading() {
            $("#output-loading").hide();
            $("#output-container").removeClass("loading");
        }

        function base64ToArrayBuffer(base64) {
            var binaryString = atob(base64);
            var len = binaryString.length;
            var bytes = new Uint8Array(len);
            for (var i = 0; i < len; i++) {
                bytes[i] = binaryString.charCodeAt(i);
            }
            return bytes;
        }

        function createCHeader(base64) {
            var decoded = base64ToArrayBuffer(base64);
            var bytes = "";
            for (var i = 0; i < decoded.byteLength; i += 16) {
                var end = Math.min(i + 16, decoded.byteLength);
                var ascii = "";
                for (var j = i; j < end; j++) {
                    bytes += "0x" + decoded[j].toString(16).padStart(2, "0");
                    if (decoded[j] >= 32 && decoded[j] <= 126) {
                        ascii += String.fromCharCode(decoded[j]);
                    } else {
                        ascii += ".";
                    }
                    if (j < decoded.byteLength - 1) {
                        bytes += ", ";
                    }
                }

                bytes += " // " + ascii;

                if (i + 16 < decoded.byteLength - 1) {
                    bytes += "\n    ";
                }
            }
            return `static const uint8_t shader[${decoded.byteLength}] =
{
    ${bytes}
};
`; 
        }

        $.ajax({
            url: window.CompileUrl,
            type: "POST",
            data: JSON.stringify(jsonObject),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            error: function (response) {
                finishLoading();
                alert("Unexpected error");
            },
            success: function (responses) {
                finishLoading();

                function selectStep() {
                    var response = responses.results[outputStepsSelect.selectedIndex];

                    var downloadButton = document.getElementById('download-button');

                    if (response.binaryOutput !== null) {
                        downloadButton.classList.remove('invisible');

                        var downloadBinaryButton = document.getElementById('download-binary-button');
                        downloadBinaryButton.href = `data:text/plain;base64,${response.binaryOutput}`;
                        downloadBinaryButton.download = 'shader-binary.o';

                        var downloadCHeaderButton = document.getElementById('download-c-header-button');
                        downloadCHeaderButton.href = `data:text/plain,${encodeURIComponent(createCHeader(response.binaryOutput))}`;
                        downloadCHeaderButton.download = 'shader.h';
                    } else {
                        downloadButton.classList.add('invisible');
                    }

                    var outputSize = document.getElementById('output-size');
                    if (response.outputSize !== null) {
                        outputSize.innerText = response.outputSize;
                        outputSize.classList.remove('invisible');
                    } else {
                        outputSize.classList.add('invisible');
                    }

                    var selectedOutputTab = document.querySelector('input[name="output-tab-selector"]:checked');
                    var selectedOutputTabIndex;
                    if (response.selectedOutputIndex !== null) {
                        selectedOutputTabIndex = response.selectedOutputIndex;
                    } else if (selectedOutputTab !== null) {
                        selectedOutputTabIndex = response.outputs.findIndex(x => x.displayName === selectedOutputTab.value);
                        if (selectedOutputTabIndex === -1) {
                            selectedOutputTabIndex = 0;
                        }
                    } else {
                        selectedOutputTabIndex = 0;
                    }

                    while (outputTabsSelect.options.length > 0) {
                        outputTabsSelect.options.remove(0);
                    }

                    var currentScrollY = (outputEditor !== null)
                        ? outputEditor.getScrollInfo().top
                        : null;

                    outputTabsSelect.onchange = () => {
                        var output = response.outputs.find(x => x.displayName === outputTabsSelect.selectedOptions[0].value);

                        outputContainerDiv.innerHTML = '';

                        switch (output.language) {
                            case "graphviz":
                                const workerURL = '/lib/vizjs/full.render.js';
                                let viz = new Viz({ workerURL });

                                viz.renderSVGElement(output.value)
                                    .then(function (element) {
                                        outputContainerDiv.appendChild(element);
                                        var panZoom = svgPanZoom(element, {
                                            controlIconsEnabled: true
                                        });

                                        function resizePanZoom() {
                                            if (element.parentElement === null) {
                                                return;
                                            }
                                            element.setAttribute('width', outputContainerDiv.offsetWidth);
                                            element.setAttribute('height', outputContainerDiv.offsetHeight);
                                            panZoom.resize();
                                            panZoom.fit();
                                            panZoom.center();
                                        }

                                        resizePanZoom();

                                        $(window).resize(resizePanZoom);
                                    })
                                    .catch(error => {
                                        outputContainerDiv.innerText = 'Could not create graph: ' + error;
                                    });
                                break;

                            case "jsontable":
                                let jsonTable = JSON.parse(output.value);
                                if (jsonTable !== null) {
                                    let tableElement = document.createElement('table');
                                    tableElement.classList.add("table", "table-sm");

                                    let headerRowElement = document.createElement('tr');
                                    for (let jsonHeaderData of jsonTable.Header.Data) {
                                        let headerElement = document.createElement('th');
                                        headerElement.innerText = jsonHeaderData;
                                        headerRowElement.appendChild(headerElement);
                                    }
                                    tableElement.appendChild(headerRowElement);

                                    for (let jsonRow of jsonTable.Rows) {
                                        let tableRowElement = document.createElement('tr');
                                        for (let jsonRowData of jsonRow.Data) {
                                            let cellElement = document.createElement('td');
                                            cellElement.innerText = jsonRowData;
                                            tableRowElement.appendChild(cellElement);
                                        }
                                        tableElement.appendChild(tableRowElement);
                                    }

                                    let tableContainerElement = document.createElement('div');
                                    tableContainerElement.style.overflowX = "auto";
                                    tableContainerElement.appendChild(tableElement);

                                    outputContainerDiv.appendChild(tableContainerElement);
                                }
                                break;

                            default:
                                if (output.language) {
                                    // No error occured and language information exists, use CodeMirror for good formatting
                                    outputEditor = CodeMirror(outputContainerDiv, {
                                        value: output.value || "",
                                        mode: getCodeMirrorMode(output.language),
                                        theme: codeMirrorTheme,
                                        matchBrackets: true,
                                        readOnly: true
                                    });
                                    if (currentScrollY !== null) {
                                        outputEditor.scrollTo(null, currentScrollY);
                                        currentScrollY = null;
                                    }
                                } else {
                                    // An error occured, allow for coloring of ansi error output
                                    // Even if an error didn't occur, there's no language so CodeMirror will struggle
                                    outputContainerDiv.innerHTML = "<pre>" + (new AnsiUp()).ansi_to_html(output.value || "") + "</pre>";
                                }
                                break;
                        }
                    };

                    for (var i = 0; i < response.outputs.length; i++) {
                        var output = response.outputs[i];
                        var isSelected = i === selectedOutputTabIndex;
                        outputTabsSelect.options.add(new Option(output.displayName, output.displayName, isSelected, isSelected));
                    }

                    outputTabsSelect.onchange();
                }

                while (outputStepsSelect.options.length > 0) {
                    outputStepsSelect.options.remove(0);
                }

                var compilationSummaryButton = document.getElementById('compilation-summary-button');
                compilationSummaryButton.classList.remove('invisible');

                var compilationSummaryContent = document.getElementById('compilation-summary-content');
                compilationSummaryContent.innerHTML = '';

                let compilationSummaryRowTemplate = document.getElementById("compilation-summary-row-template");

                var selectedStep = 0;
                var error = false;

                for (var i = 0; i < responses.results.length; i++) {
                    var step = responses.results[i];
                    if (!error) {
                        selectedStep = i;
                    }
                    error = error || !step.success;
                    var compilerEditor = compilerEditors.compilerEditors[i];
                    outputStepsSelect.options.add(
                        new Option(compilerEditor.fullDisplayName, compilerEditor.fullDisplayName, false, false));

                    let compilationSummaryRow = document.importNode(compilationSummaryRowTemplate.content, true);
                    compilationSummaryRow.querySelector("[data-field='compiler']").innerText = compilerEditor.fullDisplayName;
                    compilationSummaryRow.querySelector("[data-field='result']").innerText = step.success ? "Succeeded" : "Failed";
                    compilationSummaryRow.querySelector("[data-field='output-language']").innerText = compilerEditor.outputLanguage;
                    compilationSummaryRow.querySelector("[data-field='output-size']").innerText = step.outputSize;
                    compilationSummaryContent.appendChild(compilationSummaryRow);
                }

                outputStepsSelect.selectedIndex = selectedStep;
                outputStepsSelect.onchange = selectStep;

                outputStepsSelect.onchange();
            }
        });
    }

    var compileCodeTimeout;
    var initialized = false;

    function somethingChanged() {
        if (!initialized) {
            return;
        }

        if (window.location.search !== "") {
            window.history.pushState(null, null, window.location.origin + window.location.pathname);
        }

        clearTimeout(compileCodeTimeout);
        compileCodeTimeout = setTimeout(compileCode, 2000);
    }

    codeEditor.on('change', () => somethingChanged());

    /** @param {HTMLSelectElement} element - */
    function raiseSelectElementChange(element) {
        var event = document.createEvent('Event');
        event.initEvent('change');
        element.dispatchEvent(event);
    }

    class CompilerEditorCollection {
        constructor() {
            this.element = document.getElementById("compiler-editors");

            /** @type {CompilerEditor[]} */
            this.compilerEditors = [];
        }

        getPrevious(compilerEditor) {
            var index = this.compilerEditors.indexOf(compilerEditor);
            return (index !== 0)
                ? this.compilerEditors[index - 1]
                : null;
        }

        removeAfter(compilerEditor) {
            this._removeFrom(this.compilerEditors.indexOf(compilerEditor) + 1);
        }

        removeFrom(compilerEditor) {
            this._removeFrom(this.compilerEditors.indexOf(compilerEditor));
        }

        _removeFrom(index) {
            for (let i = index; i < this.compilerEditors.length; i++) {
                this.element.removeChild(this.compilerEditors[i].element);
            }
            this.compilerEditors.splice(index);
            this.updateAddButton();
        }

        reset() {
            this._removeFrom(0);
            this.add(getSelectedLanguage());
        }

        applyState(compilationSteps) {
            this._removeFrom(0);

            let inputLanguage = getSelectedLanguage();
            for (const compilationStep of compilationSteps) {
                const compilerEditor = this.add(inputLanguage);

                compilerEditor.compilerSelect.value = compilationStep.compiler;
                compilerEditor.onCompilerChanged();

                for (const argumentName in compilationStep.arguments) {
                    const parameterEditor = compilerEditor.parameterEditors.find(x => x.parameter.name === argumentName);
                    parameterEditor.value = compilationStep.arguments[argumentName];
                    parameterEditor.inputElement.dispatchEvent(new Event('input'));
                }

                const inputLanguageName = compilerEditor.outputLanguage;
                if (inputLanguageName !== null) {
                    inputLanguage = { name: inputLanguageName };
                }
            }
        }

        add(language) {
            const compilerEditor = new CompilerEditor(language);
            this.element.appendChild(compilerEditor.element);
            this.compilerEditors.push(compilerEditor);

            compilerEditor.element.querySelector('.lead').innerText = compilerEditor.displayName;

            compilerEditor.onCompilerChanged();

            this.updateAddButton();

            return compilerEditor;
        }

        updateAddButton() {
            var addCompilerButtonContainer = document.getElementById('add-compiler-button-container');
            var addCompilerButton = document.getElementById('add-compiler-button');

            addCompilerButtonContainer.classList.add('d-none');

            if (this.compilerEditors.length === 0) {
                return;
            }

            // If last compiler has an output that can be accepted as an input by a compiler,
            // show the Add Compiler button.
            let lastCompiler = this.compilerEditors[this.compilerEditors.length - 1];
            let outputLanguage = lastCompiler.outputLanguage;
            if (outputLanguage !== null) {
                let compiler = shaderCompilers.find(x => x.inputLanguages.includes(outputLanguage));
                if (compiler !== undefined) {
                    addCompilerButtonContainer.classList.remove('d-none');
                    addCompilerButton.onclick = () => {
                        compilerEditors.add({ name: outputLanguage });
                        return false;
                    };
                }
            }
        }
    }

    let compilerEditors = new CompilerEditorCollection();

    class CompilerEditor {
        /**
         * @param {ShaderLanguage} inputLanguage
         */
        constructor(inputLanguage) {
            this.inputLanguage = inputLanguage;

            /** @type {HTMLTemplateElement} */
            let template = document.getElementById("compiler-editor-template");

            let element = document.importNode(template.content, true).querySelector(".compiler-container");

            let compilerSelectId = `compiler-select-${uniqueId()}`;
            element.querySelector("label").htmlFor = compilerSelectId;

            element.querySelector("[data-remove-link]").onclick = () => {
                compilerEditors.removeFrom(this);
                somethingChanged();
                return false;
            };

            let compilerSelect = element.querySelector("select[data-compiler-select]");
            compilerSelect.id = compilerSelectId;
            for (let compiler of shaderCompilers) {
                if (compiler.inputLanguages.includes(inputLanguage.name)) {
                    compilerSelect.options.add(new Option(compiler.displayName, compiler.name));
                }
            }
            this.compilerSelect = compilerSelect;

            this.versionSelect = element.querySelector("div[data-version-select]");
            this.versionDescription = element.querySelector("[data-version-description]");

            this.infoButton = element.querySelector("button[data-info-button]");
            this.cardBody = element.querySelector(".card-body");
            this.argumentsDiv = element.querySelector("[data-arguments]");

            /** @type {ParameterEditor[]} */
            this.parameterEditors = [];

            compilerSelect.addEventListener("change", () => this.onCompilerChanged());

            this.element = element;
        }

        get selectedCompiler() {
            return shaderCompilers.find(x => x.name === this.compilerSelect.selectedOptions[0].value);
        }

        onCompilerChanged() {
            let compiler = this.selectedCompiler;

            this.argumentsDiv.innerHTML = '';
            this.parameterEditors.length = 0;

            if (compiler.parameters.filter(x => x.name !== "Version").length === 0) {
                this.cardBody.classList.add('d-none');
            } else {
                this.cardBody.classList.remove('d-none');
            }

            for (let parameter of compiler.parameters) {
                let parameterEditor = ParameterEditor.create(parameter);

                parameterEditor.inputElement.addEventListener(
                    'input',
                    () => {
                        if (parameterEditor.isLanguageOutput) {
                            compilerEditors.removeAfter(this);
                        }
                        somethingChanged();
                    });

                this.parameterEditors.push(parameterEditor);

                if (parameterEditor.isVersion) {
                    this.versionSelect.innerHTML = '';
                    this.versionSelect.appendChild(parameterEditor.element.querySelector("select"));
                    this.versionDescription.innerText = parameter.description;
                } else {
                    let newlyAddedElement;
                    if (parameter.filter === null || parameter.filter.name !== "__InputLanguage" || parameter.filter.values.includes(this.inputLanguage.name)) {
                        this.argumentsDiv.appendChild(parameterEditor.element);
                        newlyAddedElement = this.argumentsDiv.lastElementChild;
                    }
                    if (parameter.filter !== null && parameter.filter.name !== "__InputLanguage") {
                        let otherParameterEditor = this.parameterEditors.find(x => x.parameter.name === parameter.filter.name);
                        function updateVisibility() {
                            if (parameter.filter.values.includes(otherParameterEditor.value))
                                newlyAddedElement.classList.remove('d-none');
                            else
                                newlyAddedElement.classList.add('d-none');
                        }
                        otherParameterEditor.inputElement.addEventListener('input', updateVisibility);
                        updateVisibility();
                    }
                }
            }

            this.infoButton.onclick = () => {
                var dialog = document.querySelector('#compiler-info-dialog');
                dialog.querySelector(".modal-title").innerText = compiler.displayName;
                dialog.querySelector("[data-link]").innerText = compiler.url;
                dialog.querySelector("[data-link]").href = compiler.url;
                dialog.querySelector("[data-description]").innerText = compiler.description;

                /** @type {HTMLUListElement} */
                var inputFormats = dialog.querySelector("[data-inputs]");
                inputFormats.innerHTML = "";
                for (var language of compiler.inputLanguages) {
                    var listElement = document.createElement("li");
                    listElement.innerText = language;
                    inputFormats.appendChild(listElement);
                }

                /** @type {HTMLUListElement} */
                var outputFormats = dialog.querySelector("[data-outputs]");
                outputFormats.innerHTML = "";

                if (this.languageOutputParameter !== null) {
                    dialog.querySelector("[data-outputs-header]").classList.remove("d-none");

                    for (var language of this.languageOutputParameter.parameter.options) {
                        var listElement = document.createElement("li");
                        listElement.innerText = language;
                        outputFormats.appendChild(listElement);
                    }
                } else {
                    dialog.querySelector("[data-outputs-header]").classList.add("d-none");
                }

                $('#compiler-info-dialog').modal({});
            };

            // If this is not the first compiler, set its parameter values to match those of the previous compiler.
            let previousCompiler = compilerEditors.getPrevious(this);
            if (previousCompiler !== null) {
                for (let parameterEditor of this.parameterEditors) {
                    if (parameterEditor.isLanguageOutput || parameterEditor.isVersion) {
                        continue;
                    }
                    let previousParameterEditor = previousCompiler.parameterEditors.find(x => x.parameter.name === parameterEditor.parameter.name);
                    if (previousParameterEditor !== undefined && previousParameterEditor.element.parentElement !== null) {
                        parameterEditor.value = previousParameterEditor.value;
                    }
                }
            }

            compilerEditors.removeAfter(this);
            somethingChanged();
        }

        get languageOutputParameter() {
            let languageOutputParameter = this.parameterEditors.find(x => x.isLanguageOutput);
            return (languageOutputParameter !== undefined)
                ? languageOutputParameter
                : null;
        }

        get outputLanguage() {
            let languageOutputParameter = this.languageOutputParameter;
            return (languageOutputParameter !== null)
                ? languageOutputParameter.value
                : null;
        }

        get displayName() {
            return `Compiler #${(compilerEditors.compilerEditors.indexOf(this) + 1)}`;
        }

        get fullDisplayName() {
            return `#${(compilerEditors.compilerEditors.indexOf(this) + 1)} - ${this.selectedCompiler.displayName}`;
        }
    }

    function loadFromUrl() {
        if (window.location.pathname.length < 2) {
            return false;
        }

        var gistId = window.location.pathname.substring(1);

        $.ajax({
            url: `https://api.github.com/gists/${gistId}`,
            type: "GET",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            error: function (response) {
                alert("Unexpected error loading gist");
            },
            success: function (response) {
                const configJson = JSON.parse(response.files["config.json"].content);

                const shaderLanguage = shaderLanguages.find(x => x.name === configJson.language);
                const fileExtension = shaderLanguage.fileExtension;

                const code = response.files[`shader.${fileExtension}`].content;
                codeEditor.setValue(code);

                languageSelect.value = configJson.language;
                compilerEditors.applyState(configJson.compilationSteps);

                compileCode();

                initialized = true;
            }
        });

        return true;
    }

    languageSelect.addEventListener(
        "change",
        () => {
            let selectedLanguage = getSelectedLanguage();
            codeEditor.setValue(selectedLanguage.defaultCode);
            codeEditor.setOption("mode", getCodeMirrorMode(selectedLanguage.name));
            compilerEditors.reset();
            somethingChanged();
        });

    if (!loadFromUrl()) {
        initialized = true;
        raiseSelectElementChange(languageSelect);
    }    

    /** @type {HTMLInputElement} */
    let permalinkTextbox = document.getElementById("permalink-textbox");

    /** @type {HTMLButtonElement} */
    let copyPermalinkButton = document.getElementById('copy-permalink-button');

    document.getElementById("share-button").onclick = () => {
        var jsonObject = createJsonRequestObject();

        permalinkTextbox.value = '';
        permalinkTextbox.placeholder = 'Loading...';
        copyPermalinkButton.textContent = 'Copy';

        gtag('event', 'CreatePermalink');

        function finishLoading() {
            permalinkTextbox.placeholder = '';
        }

        $.ajax({
            url: window.CreateGistUrl,
            type: "POST",
            data: JSON.stringify(jsonObject),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            error: function (response) {
                finishLoading();
                alert("Unexpected error");
            },
            success: function (response) {
                finishLoading();
                permalinkTextbox.value = `${window.location.origin}/${response}`;
                permalinkTextbox.select();
                window.history.pushState(null, null, permalinkTextbox.value);
            }
        });

        $('#share-dialog').modal({});

        return false;
    };

    document.getElementById('copy-permalink-button').onclick = () => {
        permalinkTextbox.select();
        var copied = false;
        try {
            copied = document.execCommand('copy');
        } catch (err) {

        }

        copyPermalinkButton.textContent = copied ? "Copied" : "Couldn't copy";
        
        return false;
    };

    document.getElementById("changelog-button").onclick = () => {
        document.getElementById('changelog-content').innerText = 'Loading changelog...';

        $.get('https://raw.githubusercontent.com/tgjones/shader-playground/master/CHANGELOG.md', data => {
            var requestObject = {
                text: data,
                mode: 'gfm',
                context: 'tgjones/shader-playground'
            };

            $.ajax({
                url: `https://api.github.com/markdown`,
                type: "POST",
                data: JSON.stringify(requestObject),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                error: function (response) {
                    document.getElementById('changelog-content').innerHTML = response.responseText;
                },
                success: function (response) {
                    document.getElementById('changelog-content').innerHTML = response.responseText;
                }
            });
        });

        $('#changelog-dialog').modal({});

        return false;
    };

    document.getElementById("compilation-summary-button").onclick = () => {
        $('#compilation-summary-dialog').modal({});
    };
});
