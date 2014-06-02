define([
  'orion/assert',
  'orion/Deferred', 
  'orion/testHelpers', 
  'orion/editor/textView', 
  'orion/editor/textModel',
  'orion/editor/AsyncStyler',
  'orion/editor/editor',
  'orion/editor/contentAssist',
  'orion/editor/editorFeatures',
  'orion/serviceregistry', 
  'orion/EventTarget',
  './app'],
  function(assert, Deferred, testHelpers, mTextView,
    mTextModel, AsyncStyler, mEditor, mContentAssist,
    mEditorFeatures, mServiceRegistry, EventTarget, app) {
  'use strict';

  var serviceRegistry = new mServiceRegistry.ServiceRegistry();

  var _contentType;
  var highlightService = {
    dispatchStyleReady: function(styles) {
      this.dispatchEvent({
        type: 'orion.edit.highlighter.styleReady',
        lineStyles: styles
      });
    },

    setContentType: function(contentType) {
      _contentType = contentType;
      console.info('Set content-type to: %s', contentType);
    }
  };

  EventTarget.attach(highlightService);
  serviceRegistry.registerService('orion.edit.highlighter', highlightService, {
    type: 'highlighter'
  });

  window.setTimeout(function() {
    app.routeHighlight(function(styles) {
      highlightService.dispatchStyleReady(styles);
    });
  }, 10);

  var update = 0;
  var _textView;
  function syntaxHighlight(e) {
    //var text = _textView.getText();
    var text = _textView.getText(e.start, e.start + e.addedCharCount);
    var start = e.start;
    var removed = e.removedCharCount;
    var added = e.addedCharCount;
    app.updateCode({
      text: text,
      start: start,
      removed: removed,
      added: added,
      update: update++
    });
  }

  function unset(e) {
    _textView.removeEventListener('ModelChanged', syntaxHighlight);
    _textView.removeEventListener('Destroy', unset);
  }


  return {
    create: function(node, options) {
      var textViewFactory = function() {
        return new mTextView.TextView({
          parent: node,
          model: new mTextModel.TextModel(''),
          tabSize: 4,
          readonly: false,
          fullSelection: true,
          tabMode: true,
          expandTab: true,
          singleMode: false,
          themeClass: undefined,
          theme: undefined,
          wrapMode: false,
          wrapable: false
        });
      };

      var contentAssist, contentAssistFactory;
      contentAssistFactory = {
        createContentAssistMode: function(editor) {
          contentAssist = new mContentAssist.ContentAssist(editor.getTextView());
          var contentAssistWidget = new mContentAssist.ContentAssistWidget(contentAssist);
          var result = new mContentAssist.ContentAssistMode(contentAssist, contentAssistWidget);
          contentAssist.setMode(result);
          return result;
        }
      };

      var syntaxHighlighter = {
        styler: null,

        highlight: function(editor) {
          if(this.styler && this.styler.destroy) {
            this.styler.destroy();
          }
          this.styler = null;

          var textView = editor.getTextView();
          var annotationModel = editor.getAnnotationModel();
          this.styler = new AsyncStyler(textView, serviceRegistry, annotationModel);
          _textView = textView;
          textView.addEventListener('ModelChanged', syntaxHighlight);
          textView.addEventListener('Destroy', unset);
        }
      };

      var editor = new mEditor.Editor({
        textViewFactory: textViewFactory,
        undoStackFactory: new mEditorFeatures.UndoFactory(),
        annotationFactory: new mEditorFeatures.AnnotationFactory(),
        lineNumberRulerFactory: new mEditorFeatures.LineNumberRulerFactory(),
        foldingRulerFactory: new mEditorFeatures.FoldingRulerFactory(),
        textDNDFactory: new mEditorFeatures.TextDNDFactory(),
        contentAssistFactory: contentAssistFactory,
        keyBindingFactory: new mEditorFeatures.KeyBindingsFactory(), 
        statusReporter: options.statusReporter,
        domNode: node
      });

      editor.addEventListener('TextViewInstalled', function() {
        var sourceCodeActions = editor.getSourceCodeActions();
        sourceCodeActions.setAutoPairParentheses(true);
        sourceCodeActions.setAutoPairBraces(true);
        sourceCodeActions.setAutoPairSquareBrackets(true);
        sourceCodeActions.setAutoPairAngleBrackets(true);
        sourceCodeActions.setAutoPairQuotations(true);
        sourceCodeActions.setAutoCompleteComments(true);
        sourceCodeActions.setSmartIndentation(true);

        syntaxHighlighter.highlight(editor);
      });

      editor.setLineNumberRulerVisible(true);
      editor.setAnnotationRulerVisible(true);
      editor.setOverviewRulerVisible(true);
      editor.setFoldingRulerVisible(true);

      return editor;
    }
  };
});