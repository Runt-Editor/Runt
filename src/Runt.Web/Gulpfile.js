var gulp = require('gulp'),
    watch = require('gulp-watch'),
    rjs = require('gulp-requirejs'),
    plumber = require('gulp-plumber'),
    tcs = require('gulp-typescript-compiler'),
    clean = require('gulp-clean'),
    react = require('gulp-react'),
    stylus = require('gulp-stylus'),
    filter = require('gulp-filter');

gulp.task('build-ts', function() {
  return gulp.src('ts/**/*.js')
    .pipe(tcs({
      sourcemaps: true,
      module: 'amd'
    }))
    .pipe(gulp.dest('js'));
});

gulp.task('build-view', function() {
  return gulp.src('view/**/*.jsx')
    .pipe(react())
    .pipe(gulp.dest('js/view'));
});

gulp.task('rjs', ['build-ts', 'build-view'], function() {
  rjs({
    baseUrl: './js/',
    name: 'app',
    out: 'app.js',
    paths: {
      react: 'empty:',
      'orion': '../lib/orion.client/bundles/org.eclipse.orion.client.core/web/orion',
      'orion/editor': '../lib/orion.client/bundles/org.eclipse.orion.client.editor/web/orion/editor',
      'webtools': '../lib/orion.client/bundles/org.eclipse.orion.client.webtools/web/webtools',
      'orion/webui': '../lib/orion.client/bundles/org.eclipse.orion.client.ui/web/orion/webui',

      'i18n': '../lib/orion.client/bundles/org.eclipse.orion.client.core/web/requirejs/i18n'
    }
  }).pipe(gulp.dest('dist'));
});

gulp.task('rconf', ['build-ts'], function() {
  gulp.src('js/conf.js')
    .pipe(gulp.dest('dist'));
});

gulp.task('css', function() {
  return gulp.src('style/app.stylus')
    .pipe(stylus())
    .pipe(gulp.dest('dist'));
});

gulp.task('build', ['rjs', 'css', 'rconf']);

gulp.task('watch', function() {
  watch({glob: 'ts/**/*.ts'})
    .pipe(plumber())
    .pipe(tcs({
      sourcemaps: true,
      module: 'amd'
    }))
    .pipe(gulp.dest('js'));

  watch({glob: 'view/**/*.jsx'})
    .pipe(plumber())
    .pipe(react())
    .pipe(gulp.dest('js/view'));

  watch({glob: 'script/**/*.js'})
    .pipe(plumber())
    .pipe(gulp.dest('js'));

  watch({glob: 'style/**/*.styl', emit: 'all'}, function(files) {
    try {
      files
        .pipe(plumber())
        .pipe(filter(function(file) {
          return file.path.indexOf('app.styl') !== -1;
        }))
        .pipe(stylus())
        .pipe(gulp.dest('css'));
    } catch(e) {
      console.warn(e);
    }
  });
});

gulp.task('default', ['build']);