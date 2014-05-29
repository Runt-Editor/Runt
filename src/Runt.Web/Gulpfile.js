var gulp = require('gulp'),
    watch = require('gulp-watch'),
    rjs = require('gulp-requirejs'),
    plumber = require('gulp-plumber'),
    tcs = require('gulp-typescript-compiler');

gulp.task('build-ts', function() {
  return gulp.src('ts/**/*.js')
  .pipe(tcs({
    sourcemaps: true,
    module: 'amd'
  }))
  .pipe(gulp.dest('js'));
});

gulp.task('rjs', ['build-ts'], function() {
  rjs({
    baseUrl: './js/',
    name: 'app',
    out: 'app.all.js'
  }).pipe(gulp.dest('js'));
});

gulp.task('watch', function() {
  return watch({glob: 'ts/**/*.ts'})
    .pipe(plumber())
    .pipe(tcs({
      sourcemaps: true,
      module: 'amd'
    }))
    .pipe(gulp.dest('js'));
});

gulp.task('default', ['rjs']);