var gulp = require('gulp'),
    watch = require('gulp-watch'),
    rjs = require('gulp-requirejs'),
    plumber = require('gulp-plumber'),
    tcs = require('gulp-typescript-compiler'),
    clean = require('gulp-clean'),
    react = require('gulp-react'),
    stylus = require('gulp-stylus');

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
      react: 'empty:'
    }
  }).pipe(gulp.dest('dist'));
});

gulp.task('css', function() {
  return gulp.src('style/app.stylus')
    .pipe(stylus())
    .pipe(gulp.dest('dist'));
});

gulp.task('build', ['rjs', 'css']);

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

  watch({glob: 'style/**/*.stylus'})
    .pipe(plumber())
    .pipe(stylus())
    .pipe(gulp.dest('css'));
});

gulp.task('default', ['build']);